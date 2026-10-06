# E-Commerce Microservices End-to-End Functional Audit

Based on a comprehensive end-to-end review of the frontend, backend microservices, and API gateway, the following defects and architectural mismatches have been identified resulting from the monolith-to-microservices migration.

## 1. Payments Flow Completely Broken (Missing APIs & Logical Conflict)
- **Symptom**: The frontend `PaymentService.js` attempts to hit `/api/payments`, `/api/payments/order/{id}`, and `/api/payments/{id}/process` when viewing `OrderDetails.jsx`. These calls fail with `404 Not Found`.
- **Root Cause**: 
  1. The API Gateway (`ocelot.json`) has no upstream routes defined for `/api/payments`.
  2. The `OrderService` lacks a `PaymentsController`. The original Monolith's `PaymentsController.cs` was not migrated.
  3. **Architectural Conflict**: The `OrderService.Application.Services.OutboxService` automatically creates a dummy `Payment` record silently in the background when processing the `OrderCreated` event, effectively bypassing the manual payment flow the React frontend expects.

## 2. Admin Order Management Non-Functional
- **Symptom**: Admin attempts to manage orders will fail.
- **Root Cause**: The frontend `adminOrderService.js` attempts to call `GET /api/orders/user/{userId}` and `PUT /api/orders/{orderId}/status`. Neither of these routes are mapped in `ocelot.json`, nor are they implemented in the `OrderService.API.Controllers.OrderController`.

## 3. Order Cancellation Non-Functional
- **Symptom**: Users clicking "Cancel Order" on the frontend will receive an error.
- **Root Cause**: The frontend `orderService.js` attempts to call `POST /api/orders/{id}/cancel`. This endpoint is neither routed in the Gateway nor implemented in `OrderService`.

## 4. Inventory Reservation Leak (Permanent Lock)
- **Symptom**: Stock quantity decreases, but an order might not exist, permanently losing stock.
- **Root Cause**: During checkout, `ProductService`'s `InventoryService.ReserveInventoryAsync` deducts the stock quantity and creates an `InventoryReservation` with an `ExpiresAt` (current time + 15 mins). However, there is no background worker (e.g., `BackgroundService` or `IHostedService`) in `ProductService` to clean up or revert these reservations if they expire. If `OrderService` fails to commit an order and does not explicitly call the release endpoint, the inventory remains permanently locked.

## 5. Unattainable Admin Access (No Default Admin User)
- **Symptom**: It is impossible to log into the Admin portal on a fresh database installation.
- **Root Cause**: While `RoleConfiguration.cs` successfully seeds the `Admin` and `Customer` roles, `UserConfiguration.cs` does not seed any default Admin user account. Since the `AuthController` hardcodes new registrations to the `Customer` role, there is no application-level way to create an Admin account without direct database intervention.

## 6. Redundant/Misaligned Frontend Idempotency Handling
- **Symptom**: (Code Smell) The frontend expects `409 Conflict` on idempotent Checkout requests, but it still works because the backend correctly returns `201 Created`.
- **Root Cause**: `Checkout.jsx` has a `catch` block that specifically checks for `409` from the checkout API to handle idempotency. The backend `OrderController` correctly obeys idempotency rules by returning `201 Created` with the original order instead. While not a runtime failure, the frontend error handling is completely disconnected from the actual API contract.
