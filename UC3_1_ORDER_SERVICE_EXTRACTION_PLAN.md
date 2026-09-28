# UC3.1 Order Service Extraction Plan

## 1. Actual Codebase Findings
The original modular monolith manages Orders, Cart, Payments, and Notifications inside a unified application space sharing `ECommerceDb`.
- **Checkout Process**: Executes inside a single EF Core transaction (`IUnitOfWork.BeginTransactionAsync`). It looks up the `Cart`, resolves `Product` status, performs inventory deduction (`InventoryRepository.Update` tracking negative bounds), creates the `Order`, clears the `Cart`, updates `IdempotencyRecord`, and commits all at once.
- **Idempotency**: Utilizes an `IdempotencyRecord` entity checking `Key` and `UserId`. If successful previously, returns the cached `OrderId`. Concurrent duplicates throw `DbUpdateException`.
- **Inventory Concurrency**: Relies on EF Core throwing `DbUpdateConcurrencyException` when modifying inventory in parallel.
- **Order History (Frontend/Backend)**: The API returns `OrderItemDto` with just `ProductId`, `Quantity`, and `UnitPrice`. The frontend (`AdminOrderDetails.jsx`) performs N+1 REST calls (`getProductById`) to the API to dynamically resolve `ProductName`.
- **Product Deletion**: The `ProductService` implementation performs a **soft delete** by setting `Status = ProductStatus.Inactive`.
- **Payments**: Uses `MockPaymentService` checking against a hardcoded `MockAccountBalance = 1000000m`. Fails if amount exceeds it.
- **SignalR**: Real-time notifications operate through `OrderNotificationHub` and `OrderNotificationService` firing a `ReceiveOrderStatusUpdate` payload after state changes.

## 2. Exact Relevant File Paths
Current Monolithic Files to be ported/adapted:
- **Entities**: 
  - `ECommerce.Domain/Entities/Order.cs`
  - `ECommerce.Domain/Entities/OrderItem.cs`
  - `ECommerce.Domain/Entities/Cart.cs`
  - `ECommerce.Domain/Entities/CartItem.cs`
  - `ECommerce.Domain/Entities/Payment.cs`
  - `ECommerce.Domain/Entities/IdempotencyRecord.cs`
- **Services**: 
  - `ECommerce.Application/Services/OrderService.cs`
  - `ECommerce.Application/Services/CartService.cs`
  - `ECommerce.Application/Services/PaymentService.cs`
  - `ECommerce.Application/Services/MockPaymentService.cs`
  - `ECommerce.API/Services/OrderNotificationService.cs`
- **Controllers**: 
  - `ECommerce.API/Controllers/OrdersController.cs`
  - `ECommerce.API/Controllers/CartController.cs`
  - `ECommerce.API/Controllers/PaymentsController.cs`
- **Hubs**:
  - `ECommerce.API/Hubs/OrderNotificationHub.cs`

## 3. Service Ownership
**OrderService** will exclusively own:
- Cart management & persistence.
- Checkout orchestration (idempotency, reservation coordination).
- Order creation, history, and status management.
- Payments processing (`MockPaymentService`).
- Real-time `OrderNotificationHub`.

UserService retains JWTs and Users. ProductService retains Products, Categories, and Inventory. 

## 4. Database Ownership
A completely separate database `OrderServiceDb` will be established.
- **Tables**: `Orders`, `OrderItems`, `Carts`, `CartItems`, `Payments`, `IdempotencyRecords`.
- **Structural Changes**:
  - `Order.User` and `Cart.User` navigation properties must be removed; keep only `UserId` (`Guid`).
  - `OrderItem.Product` and `CartItem.Product` navigation properties must be removed; keep only `ProductId` (`Guid`).
- **No Cross-Database FKs**: EF Core relationships must be strictly isolated to `OrderServiceDb`.

## 5. REST Contract Specifications
To decouple OrderService from ProductService, the following REST endpoints must exist in ProductService.

**Existing Endpoints in ProductService:**
- `GET /api/products/{id}`: Look up single product info.

**Proposed Endpoints in ProductService (To be built):**
- **Bulk Product Lookup**: `POST /api/products/bulk`
  - **Request**: `[guid, guid...]`
  - **Response**: `[{id, name, price, status, stockQuantity}]`
  - **Auth**: Valid internal service JWT or customer JWT.
- **Inventory Reservation**: `POST /api/inventory/reserve`
  - **Request DTO**: `ReservationRequestDto { ReservationId, List<ProductQuantity> Items }`
  - **Response DTO**: `ReservationResultDto { Success, MissingItems }`
  - **Auth**: Service-to-service admin/internal authorization.
  - **Idempotency**: Repeated requests with the same `ReservationId` return success without deducting twice.
- **Inventory Release/Compensation**: `POST /api/inventory/reserve/{reservationId}/release`
  - **Request DTO**: Empty. Releases held stock back to available pool.
  - **Auth**: Internal authorization.
- **Inventory Confirmation**: `POST /api/inventory/reserve/{reservationId}/confirm`
  - **Request DTO**: Empty. Finalizes the reservation.

## 6. Distributed Checkout Design
Since we cannot use a single `ECommerceDb` EF transaction, the checkout must transform into a REST-based Saga/orchestration flow.

**Distributed Workflow:**
1. **OrderService**: Receives checkout request, validates JWT and idempotency key.
2. **OrderService**: Loads Cart, extracts `ProductId` and quantities.
3. **OrderService**: Makes a REST call to `ProductService` (`POST /api/products/bulk`) to validate product statuses and prices.
4. **OrderService**: Generates a unique `ReservationId` (could be the Idempotency-Key or a new Guid).
5. **OrderService**: Makes a REST call to `ProductService` (`POST /api/inventory/reserve`) for atomic reservation.
6. **ProductService**: Deducts stock or fails. Returns 200 OK.
7. **OrderService**: Enters local `OrderServiceDb` EF transaction.
8. **OrderService**: Creates `Order`, `OrderItems` (snapshotting `ProductName` and `UnitPrice`), shipping info.
9. **OrderService**: Clears `Cart`. Sets `IdempotencyRecord.Succeeded = true`.
10. **OrderService**: Commits local transaction.
11. **OrderService**: *Optional* Makes REST call to confirm reservation. (For this project, the reservation can just be considered finalized if stock is deducted).

**Compensation (Failure Handling):**
If Step 7-10 fail (e.g. database crash or validation error), `OrderService` must issue a compensation REST call to `ProductService` (`POST /api/inventory/reserve/{id}/release`) to restock the items.

## 7. Idempotency Design
The existing `IdempotencyRecord` logic correctly caches `OrderId` and returns HTTP 201 with the previous response if matched.
- **Coordination**: The provided client `Idempotency-Key` handles customer duplicates.
- **Reservation Identifiers**: `OrderService` will pass the `Idempotency-Key` (or a derivative of it) to `ProductService` as the `ReservationId`. This ensures that if the REST call times out and `OrderService` retries the reservation, `ProductService` does not deduct the inventory twice.

## 8. Failure and Recovery Handling
- **OrderService crashes after reserving stock**: The stock is locked in `ProductService`. A background job in `ProductService` should eventually expire unconfirmed reservations (e.g., after 15 mins) and restore stock, OR `OrderService` must implement a recovery outbox to release orphaned reservations upon restart.
- **Compensation request fails**: If `OrderService` tries to release stock but the network fails, it must queue this task (e.g., BackgroundService/Outbox) to retry until success, ensuring eventual consistency.
- **Lost HTTP Response**: Idempotency keys on the `reserve` endpoint ensure that a blind retry by `OrderService` is perfectly safe.

## 9. Cart Design
`CartController` maps directly to `OrderServiceDb`.
- Adding items or viewing the cart requires resolving product data.
- **Avoid N+1**: Instead of querying `ProductService` once per item in the cart, `OrderService` will batch all `ProductId`s into a single `POST /api/products/bulk` call to `ProductService` to retrieve up-to-date names, prices, and stock limits before returning the `CartDto` to the frontend.

## 10. Order History Design
**Current Flaw**: `OrderItem` only saves `UnitPrice`. The frontend dynamically resolves `ProductName`. If a product is permanently deleted, the order history loses its name.
**Resolution**: 
- `ProductService` is already utilizing **Soft Deletion** (`ProductStatus.Inactive`), which prevents 404s for historical lookups.
- However, to fully decouple, `OrderItem` in `OrderServiceDb` **MUST** be extended to snapshot `ProductName` and `ProductImageUrl` at checkout.
- This breaks the frontend's dependency on active `ProductService` queries for historical `AdminOrderDetails`.

## 11. Payment Design
The `MockPaymentService` will migrate directly into `OrderService`.
- No new external microservices.
- The success threshold (`Amount <= MockAccountBalance` where balance is `1,000,000m`) will be preserved.
- `Payments` table belongs to `OrderServiceDb`.

## 12. SignalR Design
- `OrderNotificationHub` and `OrderNotificationService` will be extracted to `OrderService.API`.
- Status updates (Pending -> Confirmed -> Shipped) naturally occur inside `OrderService`, making it the logical host for the Hub.
- The frontend will eventually point to `OrderService` (or the Gateway) for WS connections.

## 13. Authentication and Internal Security
- `OrderService.API` will validate JWTs identically to `ProductService.API` using the shared secret.
- **Internal Security**: `ProductService`'s `/api/inventory/reserve` must not be open to the public. We will implement an `Internal-Service-Key` header or specific JWT claims (e.g., `Role = Service`) to authenticate server-to-server HTTP calls between `OrderService` and `ProductService`.

## 14. Migration Strategy
1. **No Code Deletion Yet**: Leave monolith exactly as is.
2. **Setup OrderService Base**: Scaffold solution, establish `OrderServiceDb`.
3. **Enhance ProductService**: Add `/bulk` and `/reserve` endpoints to the already running `ProductService`.
4. **Implement OrderService Logic**: Wire up the SAGA checkout and cart management using `HttpClient` to communicate with `ProductService`.
5. **Verify Independently**: Test the new checkout flow end-to-end entirely independent of the monolith.

## 15. Test Strategy
- **Unit Tests**: Mock `HttpClient` and repositories to test checkout failure compensations and idempotency.
- **Integration Tests**: Test EF Core interactions inside `OrderServiceDb` (Order generation, Cart state).
- **Runtime Tests**: Spin up `UserService`, `ProductService`, and `OrderService`. Send a valid checkout REST request and verify inventory deduction in `ProductServiceDb` alongside order creation in `OrderServiceDb`.

## 16. Risks and Unresolved Decisions
| Risk | Proposed Mitigation |
|------|---------------------|
| Distributed Checkout Failure | Implement a robust reservation/compensation REST Saga with idempotency keys. |
| Network Timeouts | Use Polly in `OrderService` for HTTP retries with exponential backoff for `ProductService` calls. |
| Cart N+1 Problem | Implement a `/bulk` product lookup endpoint in `ProductService`. |
| Historical Order Integrity | Snapshot `ProductName` onto the `OrderItem` entity during Checkout to sever frontend read dependency on `ProductService`. |

## 17. Proposed Implementation Phases
- **Phase A**: Scaffolding `OrderService` architecture and DbContext (`OrderServiceDb`).
- **Phase B**: Extract Entities (Order, Cart, Payment, Idempotency) and map relationships without foreign keys.
- **Phase C**: Implement the required `/bulk` and `/reserve` REST contracts inside the existing `ProductService`.
- **Phase D**: Implement internal `ProductHttpClient` in `OrderService` for lookups and reservations.
- **Phase E**: Implement Distributed Checkout workflow, Idempotency, and Compensation logic.
- **Phase F**: Migrate Carts, Payments, and SignalR.
- **Phase G**: Full multi-service runtime verification.

UC3.1 ORDER SERVICE ANALYSIS COMPLETE — NO CODE CHANGED
