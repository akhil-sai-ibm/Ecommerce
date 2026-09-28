import os

doc = """# UC3.1 MICROSERVICES DECOMPOSITION PLAN

## PART 1 — INSPECT THE CURRENT SOLUTION

### Solution Structure
The current solution `ECommerce.sln` is structured as a Modular Monolith using Clean Architecture.
- **ECommerce.Domain:** Contains core entities (`User`, `Product`, `Order`, `Inventory`, `Cart`, etc.), enums (`OrderStatus`, `ProductStatus`), and exceptions (`DomainException`).
- **ECommerce.Application:** Contains DTOs (`CheckoutRequestDto`, `ProductDto`), Service interfaces (`IOrderService`, `IProductService`), and Repository interfaces (`IOrderRepository`, `IProductRepository`). It also holds the concrete service implementations (`OrderService`, `ProductService`, `AuthService`, etc.).
- **ECommerce.Infrastructure:** Implements DB contexts (`ECommerceDbContext`), EF Core configurations (`OrderConfiguration`, `ProductConfiguration`), concrete repositories (`OrderRepository`, `ProductRepository`), JWT logic (`JwtTokenService`), Background Jobs (`LowStockBackgroundService`), File Storage (`LocalFileStorageService`), and caching (`CacheService`).
- **ECommerce.API:** The presentation layer containing Controllers (`OrdersController`, `ProductsController`, `AuthController`) and configuration (`Program.cs`, `appsettings.json`).
- **frontend:** React/Vite application interacting with the single ASP.NET Core API backend.
- **Tests:** `ECommerce.UnitTests` and `ECommerce.IntegrationTests` verifying monolithic workflows.

### Feature Implementations
- **Authentication:** `AuthService`, `JwtTokenService`, `UserController`.
- **Payment:** `MockPaymentService` (deterministic mock implementation).
- **Inventory:** `InventoryService`, `InventoryRepository`, `LowStockBackgroundService`.
- **Cart:** `CartService`, `CartRepository`.
- **Order:** `OrderService`, `OrderRepository`.
- **Notification:** `IOrderNotificationService` (SignalR), `LowStockBackgroundService`.
- **SignalR:** `OrderNotificationHub` mapped in `ECommerce.API`.
- **Caching:** `CacheService` using `IMemoryCache` (Categories).
- **Idempotency:** `IdempotencyRepository` tracking unique keys for checkout.
- **File/Image Storage:** `LocalFileStorageService` saving to `wwwroot/uploads`.

---

## PART 2 — CURRENT ARCHITECTURE

**Current Flow:**
```text
React
  ↓ (Axios HTTP calls)
ASP.NET API (Controllers)
  ↓ (Dependency Injection)
Application (Services, DTOs, Business Rules)
  ↓ (Interface implementation inversion)
Infrastructure (Repositories, EF Core context)
  ↓ (LINQ translated to SQL)
EF Core
  ↓ (ADO.NET)
SQL Server (Single Database)
```

**Current Module Boundaries & Dependencies:**
Inside the monolith, domains heavily depend on each other at the database and application level:
- `OrderService` queries `CartRepository`, `ProductRepository`, `InventoryRepository`, and `IdempotencyRepository` directly.
- `CartService` queries `ProductRepository` and `InventoryRepository`.
- EF Core navigation properties (e.g., `OrderItem.Product`, `CartItem.Product`, `Order.User`) span across distinct business domains.
- A single atomic transaction (`IUnitOfWork.SaveChangesAsync()`) wraps operations involving multiple domains (e.g., deducting inventory and creating an order).

---

## PART 3 — PROPOSED MICROSERVICE BOUNDARIES

### 1. Product Service
- **Responsibilities:** Catalog management, categories, inventory management, low-stock background checks, product image handling.
- **Entities Owned:** `Product`, `Category`, `Inventory`, `Notification` (for low stock).
- **DTOs Owned:** `ProductDto`, `CategoryDto`, `InventoryDto`.
- **Controllers Owned:** `ProductsController`, `CategoriesController`, `InventoryController`.
- **Application Services:** `ProductService`, `CategoryService`, `InventoryService`, `LocalFileStorageService` (for product images), `LowStockBackgroundService`.
- **Repositories:** `ProductRepository`, `CategoryRepository`, `InventoryRepository`.
- **Database Tables Owned:** `Products`, `Categories`, `Inventories`, `Notifications`.
- **REST APIs Exposed:** `/api/products/*`, `/api/categories/*`, `/api/inventory/*`.
- **REST APIs Called:** None currently (it is an upstream service).

### 2. User Service
- **Responsibilities:** Registration, Authentication (JWT), Profile management, Profile image handling, Addresses.
- **Entities Owned:** `User`, `Role`, `Address`.
- **DTOs Owned:** `UserDto`, `RegisterUserDto`, `UpdateUserDto`.
- **Controllers Owned:** `AuthController`, `UsersController`.
- **Application Services:** `AuthService`, `UserService`, `JwtTokenService`, `LocalFileStorageService` (for profile images).
- **Repositories:** `UserRepository`, `RoleRepository` (if separate).
- **Database Tables Owned:** `Users`, `Roles`, `Addresses`.
- **REST APIs Exposed:** `/api/auth/*`, `/api/users/*`.
- **REST APIs Called:** None.

### 3. Order Service
- **Responsibilities:** Cart management, Checkout process, Order history, Order status updates, Payments, SignalR notifications to users.
- **Entities Owned:** `Order`, `OrderItem`, `Cart`, `CartItem`, `Payment`, `IdempotencyRecord`.
- **DTOs Owned:** `OrderDto`, `CartDto`, `CheckoutRequestDto`, `PaymentDto`.
- **Controllers Owned:** `OrdersController`, `CartController`, `PaymentsController`.
- **Application Services:** `OrderService`, `CartService`, `PaymentService`, `OrderNotificationService` (SignalR).
- **Repositories:** `OrderRepository`, `CartRepository`, `PaymentRepository`, `IdempotencyRepository`.
- **Database Tables Owned:** `Orders`, `OrderItems`, `Carts`, `CartItems`, `Payments`, `IdempotencyRecords`.
- **REST APIs Exposed:** `/api/orders/*`, `/api/cart/*`, `/api/payments/*`, `/hubs/orders` (SignalR).
- **REST APIs Called:** 
  - Call Product Service: `GET /api/products/{id}` (to validate items).
  - Call Product Service: `PUT /api/inventory/deduct` (to update stock during checkout).

*Note: Payments and Notifications (SignalR for orders) remain in the Order Service as they are tightly coupled to the Order lifecycle in this application scope.*

---

## PART 4 — ENTITY OWNERSHIP

| Entity | Current Location | Proposed Service | Reason |
|--------|------------------|------------------|-------|
| User | ECommerce.Domain | User Service | Centralized authentication and identity. |
| Role | ECommerce.Domain | User Service | Tied directly to Users. |
| Address| ECommerce.Domain | User Service | Belongs to the User profile. |
| Product | ECommerce.Domain | Product Service | Core catalog entity. |
| Category | ECommerce.Domain | Product Service | Classifies products. |
| Inventory | ECommerce.Domain | Product Service | 1:1 mapping with Product stock. |
| Cart | ECommerce.Domain | Order Service | Pre-cursor to an Order. |
| CartItem | ECommerce.Domain | Order Service | Linked to Cart. |
| Order | ECommerce.Domain | Order Service | Core transactional entity. |
| OrderItem | ECommerce.Domain | Order Service | Line items of an Order. |
| Payment | ECommerce.Domain | Order Service | Resolves an Order transaction. |
| IdempotencyRecord | ECommerce.Domain | Order Service | Protects checkout requests. |
| Notification | ECommerce.Domain | Product Service | Used primarily for low-stock alerts. |

**Relationships Broken by Decomposition:**
- `Order.User`: Will become a `Guid UserId`. No navigation property.
- `Cart.User`: Will become a `Guid UserId`. No navigation property.
- `OrderItem.Product`: Will become a `Guid ProductId`. No navigation property.
- `CartItem.Product`: Will become a `Guid ProductId`. No navigation property.
- `Address.User`: Remains a navigation property (both in User Service).
- `Product.Inventory`: Remains a navigation property (both in Product Service).

---

## PART 5 — DATABASE OWNERSHIP

**Product Service DB:**
- `Products`, `Categories`, `Inventories`, `Notifications`.
- *Access:* Only Product Service can write and read. Order Service accesses via REST.

**User Service DB:**
- `Users`, `Roles`, `Addresses`.
- *Access:* Only User Service can write and read. 

**Order Service DB:**
- `Orders`, `OrderItems`, `Carts`, `CartItems`, `Payments`, `IdempotencyRecords`.
- *Access:* Only Order Service can write and read.
- *Changes:* `OrderItems` and `CartItems` will no longer have an explicit SQL Foreign Key constraint pointing to the `Products` table. The `ProductId` will be a standard `UniqueIdentifier` field without referential integrity enforced by the database. Order includes historical pricing, so it does not need to constantly join with Products. For Cart views, Order Service will need to fetch Product details via REST or cache them.

---

## PART 6 — CROSS-SERVICE COMMUNICATION

**1. Checkout (Order → Product/Inventory)**
- *CURRENT:* `OrderService` uses `IInventoryRepository` to validate and deduct inventory directly within the same SQL transaction.
- *FUTURE:* `OrderService` must make a synchronous REST call to `Product Service` (e.g., `POST /api/inventory/deduct`) to deduct stock.

**2. Viewing Cart (Cart → Product)**
- *CURRENT:* EF Core `.Include(c => c.Product)`.
- *FUTURE:* `CartService` retrieves `CartItems` from Order DB. It then makes a REST call (or uses a localized cache) to `Product Service` to fetch product names, prices, and images to return the complete `CartDto` to the frontend.

**3. Order History (Order → User)**
- *CURRENT:* EF Core `.Include(o => o.User)`.
- *FUTURE:* Since shipping info is stored on the `Order` snapshot, `Order Service` rarely needs user info. If it does, it calls `User Service` via REST.

---

## PART 7 — API DECOMPOSITION

**PRODUCT SERVICE**
- `GET, POST, PUT, DELETE /api/products` (From `ProductsController` -> Remains, but removes Cart cleanup logic).
- `GET, POST, PUT, DELETE /api/categories` (From `CategoriesController` -> Remains).
- `GET, PUT /api/inventory` (From `InventoryController` -> Remains, but needs new endpoint `POST /api/inventory/deduct` for Order Service).

**USER SERVICE**
- `POST /api/auth/login`, `/register` (From `AuthController` -> Remains).
- `GET, PUT /api/users/profile`, `/image` (From `UsersController` -> Remains).

**ORDER SERVICE**
- `GET, POST, PUT, DELETE /api/cart` (From `CartController` -> Changes: Must fetch product data via REST instead of EF Core).
- `POST /api/orders/checkout` (From `OrdersController` -> Changes: Must implement distributed transaction/Saga for inventory deduction).
- `GET, PUT /api/orders` (From `OrdersController` -> Remains).
- `/hubs/orders` (SignalR Hub -> Remains).

---

## PART 8 — AUTHENTICATION

- **JWT Generation:** Remains in User Service (`AuthService`). Password hashing (`PBKDF2`) stays here.
- **Claims:** JWT contains `UserId`, `Email`, and `Role`.
- **Validation:** Every microservice (Product, Order, User) will configure ASP.NET Core JWT Bearer middleware using the *same Secret Key*. This allows Product and Order services to independently validate the JWT's signature without calling the User Service.
- **Trust:** Services will trust the `UserId` and `Role` extracted from the validated JWT claims.

---

## PART 9 — CHECKOUT FLOW

**Current Flow (Monolith):**
1. API receives `CheckoutRequestDto`.
2. `OrderService` checks `IdempotencyRepository`.
3. Validates cart and checks inventory directly.
4. Creates Order and deducts inventory inside `UnitOfWork.SaveChangesAsync()`. (Atomic commit).

**Proposed Flow (Microservices):**
1. API (Order Service) receives `CheckoutRequestDto`.
2. `OrderService` checks `IdempotencyRepository`.
3. Validates cart.
4. **Saga/REST step:** Order Service calls `POST ProductService/api/inventory/reserve-or-deduct` with a correlation ID.
5. Product Service validates and deducts inventory atomically in its DB, returning Success/Failure.
6. If Success: Order Service creates `Order`, clears `Cart`, commits to Order DB.
7. If Order DB commit fails: Order Service must invoke a compensating transaction (`POST ProductService/api/inventory/compensate`) to revert the stock.

*Architectural Change:* We cannot rely on EF Core distributed transactions. We must use asynchronous Sagas or explicit REST-based compensating transactions.

---

## PART 10 — RISKS AND BREAKING CHANGES

1. **Distributed Transactions (Checkout):** 
   - *Risk:* Order creation fails after stock is deducted in Product Service. 
   - *Solution:* Implement compensating REST calls or an outbox pattern.
2. **Cross-Database Foreign Keys:**
   - *Risk:* EF Core configurations map `OrderItem.Product`.
   - *Solution:* Remove FK constraints. `ProductId` becomes a plain Guid in Order DB.
3. **Cart Performance:**
   - *Risk:* Fetching Cart requires a network hop to Product Service to get names/images.
   - *Solution:* Bulk query the Product Service (`GET /api/products/bulk?ids=...`) or cache product data in Order Service.
4. **Product Deletion:**
   - *Risk:* R11 logic checks if OrderItems exist before hard-deleting a Product. Product Service no longer sees OrderItems.
   - *Solution:* Product Service must ask Order Service (`GET /api/orders/contains-product/{id}`) before allowing a physical deletion.
5. **CORS & Routing:**
   - *Risk:* React currently talks to `localhost:52549`. Now it must talk to three different ports.
   - *Solution:* React must be configured with three base URLs, OR we introduce an API Gateway (e.g., YARP) to route `/api/products` to Product Service, etc. (Though no gateway is requested yet, React services must be updated).

---

## PART 11 — RECOMMENDED PROJECT STRUCTURE

```text
/ECommerce.sln
/src/
  /ProductService
      /ProductService.API
      /ProductService.Application
      /ProductService.Domain
      /ProductService.Infrastructure
  /UserService
      /UserService.API
      /UserService.Application
      /UserService.Domain
      /UserService.Infrastructure
  /OrderService
      /OrderService.API
      /OrderService.Application
      /OrderService.Domain
      /OrderService.Infrastructure
/frontend
```
Each service gets its own clean architecture layers to maintain domain isolation.

---

## PART 12 — MIGRATION STRATEGY

**Strategy: Strangler Fig (Incremental Extraction)**

1. **Step 1: Extract User Service**
   - User identity has the fewest inbound dependencies.
   - Copy Auth and User logic to a new User Service.
   - Update monolith to validate JWTs independently.
2. **Step 2: Extract Product Service**
   - Move Products, Categories, Inventory to Product Service.
   - Update monolith (which now becomes Order Service) to remove EF Core joins to Product.
   - Implement REST calls in Order Service for Cart population and Checkout inventory deduction.
3. **Step 3: Refine Order Service**
   - The remaining monolith is effectively the Order Service. Clean up unused code and rename projects.
4. **Step 4: Frontend Updates**
   - Update Axios services in React to point to the respective new ports (e.g., Auth=5001, Product=5002, Order=5003).

---

## PART 13 — UC3.1 ACCEPTANCE CRITERIA CHECK

| Acceptance Criterion | Current State | Proposed Solution | Work Required |
|-----------------------|---------------|-------------------|---------------|
| Product Service (Catalog/Inventory) | Monolith | Separate API. Owns Products/Inventory DB. | Extract logic, remove FKs. |
| User Service (Auth/Identity) | Monolith | Separate API. Owns Users DB. | Extract JWT generation & User logic. |
| Order Service (Cart/Orders) | Monolith | Separate API. Owns Orders DB. | Implement REST compensation for checkout. |
| Individual DB Ownership | Single DB | 3 Separate SQL Server DBs/Schemas. | Separate EF Core contexts & connection strings. |
| Communicate via REST | EF Core joins | Axios/HttpClient calls between APIs. | Implement HTTP Clients in Application layer. |

---

## PART 14 — FINAL RECOMMENDATION

1. **Recommended final architecture:** Three independent Web APIs (User, Product, Order) using Clean Architecture internally.
2. **Recommended boundaries:** User (Identity), Product (Catalog/Stock), Order (Cart/Checkout/Fulfillment).
3. **Recommended database boundaries:** 3 distinct databases to guarantee no shared SQL tables.
4. **Recommended communication flow:** Synchronous REST calls using `HttpClient` (e.g., Order calling Product for inventory).
5. **Recommended extraction order:** User Service -> Product Service -> Order Service.
6. **Biggest risks:** Loss of atomic transactions during checkout; Orphaned references if a Product is deleted without checking Orders.
7. **Files to rewrite:** All `DbContext` configurations (remove cross-domain FKs), `CartService` (requires REST fetching), `OrderService` (requires REST inventory deduction), React `api.js` (needs multiple base URLs).
8. **Things that should NOT be changed:** Frontend component UI structure, core domain business rules, JWT secret configurations.
9. **What should be implemented first:** Establish the new Solution structure and extract the User Service.
10. **What should explicitly NOT be implemented yet:** Containerization (Podman/Docker), API Gateways, Service Meshes, Asynchronous Message Brokers (RabbitMQ/Kafka). Keep it to simple REST for now as requested.

UC3.1 ANALYSIS COMPLETE — NO CODE CHANGED
"""

with open('UC3.1_MICROSERVICES_PLAN.md', 'w', encoding='utf-8') as f:
    f.write(doc)
