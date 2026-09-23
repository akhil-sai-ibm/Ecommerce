import os

doc = """# FINAL ECOMMERCE PROJECT — COMPLETE CODEBASE STUDY & INTERVIEW DOCUMENTATION

## PART 1 — COMPLETE PROJECT OVERVIEW

**What the application does:**
This is a modern E-Commerce web application that allows customers to browse a product catalog, add items to a cart, proceed through a secure checkout process, and track their order history. It includes real-time notifications for order status updates.

**Who the users are:**
1. **Customers:** General users who register to buy products.
2. **Admins:** Store managers/owners who manage the catalog, inventory, and fulfill orders.

**Customer capabilities:**
- Register, login, and manage their profile (including profile picture).
- Browse and search products, view product details and stock availability.
- Manage a shopping cart (add, remove, change quantities).
- Checkout with shipping information.
- View order history and real-time order status updates.

**Admin capabilities:**
- View administrative dashboard.
- Manage products (CRUD operations, upload product images).
- Manage inventory (restock).
- Manage categories.
- Process Excel bulk uploads for products/categories.
- Manage orders (update statuses to Shipped, Delivered, etc.).
- Receive low-stock notifications.

**Main business workflows:**
- **Customer Checkout Flow:** Customer adds items to cart -> Submits shipping info -> Backend validates inventory -> Deducts stock -> Creates Order -> Clears Cart -> Returns Order ID.
- **Admin Fulfillment Flow:** Admin views pending orders -> Updates status -> Backend commits transaction -> SignalR pushes real-time notification to the customer's browser.

**Technologies used:**
- **Frontend:** React, Vite, Axios, React Router.
- **Backend API:** ASP.NET Core Web API (.NET 8/10).
- **Architecture/Patterns:** Clean Architecture, Repository Pattern, Dependency Injection.
- **Database:** SQL Server (LocalDB for dev) accessed via Entity Framework Core (EF Core).
- **Authentication:** JWT (JSON Web Tokens) with PBKDF2 Password Hashing.
- **Real-time:** SignalR.

**Why each technology was chosen:**
- *ASP.NET Core:* High performance, strongly typed (C#), built-in DI, excellent enterprise support.
- *React:* Component-based UI, massive ecosystem, efficient virtual DOM.
- *EF Core:* Official ORM for .NET, supports LINQ, migrations, and simplifies database interactions.
- *SQL Server:* Relational integrity, ACID compliance, suitable for financial/e-commerce transactions.

**Overall architecture (Clean Architecture):**
```text
React (Frontend)
  ↓ HTTP/JSON (Axios)
ASP.NET Core API (Presentation Layer - Controllers)
  ↓ DTOs & Interfaces
Application (Business Logic - Services)
  ↓ Domain Entities & Repository Interfaces
Domain (Core Entities, Enums, Exceptions)
  ↑ (Dependency Inversion)
Infrastructure (EF Core, SQL Server, File Storage, Repositories)
  ↓ SQL Queries
SQL Server (Database)
```
- **React -> API:** The browser sends HTTP requests with JWT tokens.
- **API -> Application:** Controllers validate requests and call Services.
- **Application -> Domain:** Services enforce business rules using Domain Entities.
- **Domain <- Infrastructure:** Infrastructure implements the interfaces defined in Application, depending on Domain for the entities.
- **Infrastructure -> SQL Server:** EF Core translates LINQ into SQL.

---

## PART 2 — EXACT COMPLETE FILE STRUCTURE

```text
ECommerce.sln
├── ECommerce.Domain/
│   ├── Entities/ (BaseEntity.cs, Product.cs, Order.cs, User.cs, Cart.cs, Inventory.cs, Category.cs...)
│   ├── Enums/ (OrderStatus.cs, ProductStatus.cs, UserRole.cs...)
│   └── Exceptions/ (DomainException.cs)
├── ECommerce.Application/
│   ├── DTOs/ (ProductDto.cs, CheckoutRequestDto.cs, OrderDto.cs...)
│   ├── Interfaces/
│   │   ├── Repositories/ (IProductRepository.cs, IOrderRepository.cs, IUnitOfWork.cs...)
│   │   └── Services/ (IProductService.cs, IAuthService.cs, IFileStorageService.cs...)
│   └── Services/ (ProductService.cs, OrderService.cs, AuthService.cs...)
├── ECommerce.Infrastructure/
│   ├── Data/
│   │   ├── ECommerceDbContext.cs
│   │   ├── Configurations/ (ProductConfiguration.cs, OrderConfiguration.cs...)
│   │   └── Migrations/
│   ├── Repositories/ (ProductRepository.cs, OrderRepository.cs, UnitOfWork.cs...)
│   ├── Authentication/ (JwtTokenService.cs, AspNetCorePasswordHasher.cs)
│   ├── Services/ (LocalFileStorageService.cs)
│   └── BackgroundJobs/ (LowStockBackgroundService.cs)
├── ECommerce.API/
│   ├── Controllers/ (ProductsController.cs, OrdersController.cs, AuthController.cs...)
│   ├── Program.cs
│   └── appsettings.json
├── ECommerce.UnitTests/
│   └── Services/ (BasicServiceTests.cs...)
├── ECommerce.IntegrationTests/
│   ├── ApiIntegrationTests.cs
│   ├── CartCheckoutIntegrationTests.cs
│   └── OrderStateIntegrationTests.cs
└── frontend/
    ├── src/
    │   ├── components/ (Navbar.jsx, ProductCard.jsx...)
    │   ├── context/ (AuthContext.jsx, SignalRContext.jsx)
    │   ├── pages/ (Home.jsx, Checkout.jsx, admin/AdminProducts.jsx...)
    │   └── services/ (api.js, authService.js, productService.js...)
    └── package.json
```

---

## PART 3 — DOMAIN LAYER

**Entities & Purpose:**
- **BaseEntity:** Abstract base class providing `Id` (Guid), `CreatedAt`, `UpdatedAt`.
- **User:** Represents an authenticated user (Customer/Admin). Properties: `Email`, `PasswordHash`, `FirstName`, `LastName`, `ProfileImageUrl`, `RoleId`.
- **Role:** Defines access levels (`Admin`, `Customer`).
- **Product:** The item being sold. Properties: `Name`, `Description`, `Price`, `SKU`, `ImageUrl`, `Status`. FK: `CategoryId`. Navigation: `Inventory`, `Category`.
- **Category:** Groups products.
- **Inventory:** Tracks stock. 1:1 with Product. Properties: `Quantity`, `LowStockThreshold`.
- **Cart & CartItem:** Temporary storage for items before checkout. `Cart` belongs to a `User`.
- **Order & OrderItem:** Permanent record of a purchase. Order has `ShippingAddressLine1`, `ShippingPostalCode`, `OrderDate`, `TotalAmount`. OrderItem references `ProductId` and stores historical `UnitPrice`.
- **Payment:** Tracks transaction status.
- **IdempotencyRecord:** Prevents duplicate checkout requests by storing processed `IdempotencyKey`.

**Enums:**
- `ProductStatus`: Active, Inactive (used for soft deletion).
- `OrderStatus`: Pending, Processing, Shipped, Delivered, Cancelled.
- `UserRole`: Admin, Customer.

---

## PART 4 — DATABASE DESIGN

| Table | Primary Key | Important Foreign Keys | Purpose |
|-------|-------------|------------------------|---------|
| Users | Id | RoleId | Stores user credentials and profile. |
| Products | Id | CategoryId | Stores product catalog. |
| Inventories | Id | ProductId | Tracks stock levels. 1:1 with Products. |
| Carts | Id | UserId | Active shopping cart. |
| CartItems | Id | CartId, ProductId | Items in the cart. |
| Orders | Id | UserId | Historical order records. |
| OrderItems| Id | OrderId, ProductId | Historical line items. |
| IdempotencyRecords | IdempotencyKey | None | Ensures safe retries. |

**DeleteBehavior (R11 Fix):**
`OrderItem -> Product` uses **Restrict**. We cannot physically delete a Product if it has been purchased, because the `OrderItem` must retain its historical reference.
If an Admin deletes a product with historical orders, the Application layer catches the FK restriction (or proactively checks) and performs a **Soft Deactivation** (`ProductStatus = Inactive`) instead. 
The cart items (which are transient) are physically deleted before deactivating/deleting the product.

---

## PART 5 — EF CORE

- **DbContext:** `ECommerceDbContext` manages DbSets for all entities.
- **Configurations:** Uses `IEntityTypeConfiguration<T>` in the `Configurations/` folder (Fluent API) to keep DbContext clean.
- **Include(p => p.Inventory):** Used for Eager Loading so that when we fetch a Product, we get its stock quantity in one query.
- **AsNoTracking():** Used in read-only queries (like catalog browsing) to bypass the EF Core Identity Map, reducing memory overhead and improving performance.
- **IdentityMap/Tracking Issue:** Occurred when modifying Inventory while the parent Product was already tracked. Fixed by properly detaching entities or querying directly from the tracking context instead of reloading.

---

## PART 6 — APPLICATION LAYER

- **ProductService:** Handles CRUD for products. In R11, orchestrates the deletion logic (deleting CartItems first, attempting hard delete, falling back to soft deactivate, and cleaning up the `ImageUrl` via `IFileStorageService`).
- **OrderService:** Handles the complex Checkout transaction (inventory check, deduction, order creation).
- **AuthService:** Hashes passwords, verifies credentials, generates JWTs.
- **CartService:** Manages cart items and stock validation before adding.

Flow Example:
`ProductsController.Delete(id)` -> `ProductService.DeleteAsync(id)` -> `ProductRepository.GetById()` -> `DbContext.Remove()` -> `SQL Server`.

---

## PART 7 — REPOSITORY PATTERN

- **Why Interfaces in Application?** Dependency Inversion Principle. The business logic defines *what* it needs (the interface), not *how* it's stored.
- **Why Implementations in Infrastructure?** Infrastructure handles the EF Core specifics. If we switch to Dapper or MongoDB, the Application layer doesn't change.
- **GenericRepository:** Provides basic `GetByIdAsync`, `AddAsync`, `UpdateAsync`.
- **UnitOfWork:** Groups multiple repository operations into a single EF Core transaction (`SaveChangesAsync`).

---

## PART 8 — AUTHENTICATION & AUTHORIZATION

- **Registration:** Passwords are hashed using ASP.NET Core's `PasswordHasher<T>` (PBKDF2).
- **Login:** User submits Email/Password -> `AuthService` verifies hash -> Generates JWT containing `NameIdentifier` (UserId) and `Role` claims.
- **Frontend:** React stores the JWT in `localStorage`.
- **Axios:** A request interceptor attaches `Authorization: Bearer <token>` to every outgoing API request.
- **[Authorize]:** Controllers use this to ensure the user is logged in. `[Authorize(Roles = "Admin")]` restricts to Admins.

---

## PART 9 — PRODUCT MANAGEMENT

- **Lifecycle:** Create -> Upload Image -> Set Stock -> Display in Catalog -> (Buy) -> Delete/Deactivate.
- **StockQuantity:** The `ProductDto` flattens the 1:1 relationship, projecting `Inventory.Quantity` as `StockQuantity` so the frontend gets a simple flat object.
- **R11 Image Flow:** The admin uploads an `IFormFile`. `ProductService` calls `IFileStorageService.SaveFileAsync`, which saves it to `wwwroot/uploads/products` with a generated GUID filename and returns the relative path (`/uploads/products/xyz.jpg`), which is saved in `Product.ImageUrl`.

---

## PART 10 — FILE STORAGE & IMAGES

- **LocalFileStorageService:** Manages physical files on disk.
- **wwwroot:** ASP.NET Core serves static files from this directory via `app.UseStaticFiles()`.
- **R11 FileData.Length Bug:** `IFormFile` streams must be read correctly. If a stream is read and not reset to position 0, its length/content appears as 0 to subsequent readers.
- **Why not DB?** Storing binaries in SQL Server bloats the database, slows down backups, and increases memory usage. File systems are optimized for serving static binary files.

---

## PART 11 — CUSTOMER PROFILE

- **Profile:** Users can update their `FirstName`, `LastName`, and upload a `ProfileImageUrl`.
- **Image Replacement:** If a user uploads a new profile picture, `UserService` deletes the old file using `IFileStorageService.DeleteFileAsync` to prevent orphaned files on disk.

---

## PART 12 — CART

- **Validation:** When `CartService.AddItemAsync` is called, it checks `Inventory.Quantity` to ensure the requested quantity doesn't exceed available stock.
- **Ownership:** Carts are queried by the logged-in User's ID (extracted from JWT claims).

---

## PART 13 — CHECKOUT

**Exact Flow:**
1. Frontend `Checkout.jsx` sends `CheckoutRequestDto` with `Idempotency-Key` header.
2. `OrderService` checks `IdempotencyRepository`. If key exists, returns early.
3. Retrieves User's Cart and CartItems.
4. Validates `Inventory` for every item. Throws exception if out of stock.
5. Deducts `Inventory.Quantity`.
6. Creates `Order` and maps `CartItems` to `OrderItems`.
7. Clears `CartItems`.
8. Saves `IdempotencyRecord`.
9. `UnitOfWork.SaveChangesAsync()` commits everything atomically.

**R10 Defect:** Frontend sent `shippingZipCode`, backend expected `ShippingPostalCode`. The ASP.NET model binder threw a `400 Bad Request` because `[Required]` validation failed. Fixed by aligning the frontend payload property name.

---

## PART 14 — ORDERS

- **Shipping Snapshot:** Shipping address is stored on the `Order` entity itself. If the user moves and updates their profile address in the future, the historical order must still reflect where it was actually shipped at that time.

---

## PART 15 — PAYMENTS

- **Mock Implementation:** `MockPaymentService` simulates payment gateway interactions. Based on specific criteria (like amount > $1000), it deterministically succeeds or fails to allow testing of both flows. Decoupled from checkout so it can be swapped with Stripe/PayPal later.

---

## PART 16 — SIGNALR

- **Hub:** `OrderNotificationHub` at `/hubs/orders`.
- **Flow:** Admin changes Order status -> `OrderService` updates DB -> `IOrderNotificationService` sends message to Hub -> Hub broadcasts to the specific Customer's browser -> React `SignalRContext` receives message -> Displays Toast notification.
- **Token:** SignalR requires the JWT token in the query string `?access_token=...` during the WebSocket handshake.

---

## PART 17 — BACKGROUND JOBS & NOTIFICATIONS

- **LowStockBackgroundService:** Inherits `BackgroundService`. Runs periodically to check `Inventory.Quantity <= LowStockThreshold`. Creates `Notification` records for Admins.

---

## PART 18 — CACHING

- **IMemoryCache:** Caches Category lists (`CategoryService`) because categories change rarely but are queried on every catalog page load. Cache is invalidated when an Admin creates/updates a Category.

---

## PART 19 — FRONTEND ARCHITECTURE

- **Vite + React:** Fast build tool and component library.
- **Structure:**
  - `components/`: Reusable UI (Navbar, ProductCard).
  - `pages/`: Route views (Home, Checkout, AdminProducts).
  - `services/`: Axios wrappers for API calls.
  - `context/`: Global state (Auth, SignalR).

---

## PART 20 — REACT AUTHENTICATION

- **ProtectedRoutes:** A wrapper component that checks `AuthContext`. If no JWT, redirects to `/login`.
- **401 Handling:** Axios response interceptor catches `401 Unauthorized`. It clears `localStorage` and redirects the user to login automatically, preventing infinite loops on expired tokens.

---

## PART 21 — AXIOS/API SERVICES

- **api.js:** Configures the base Axios instance with `baseURL: 'https://localhost:52549/api'` and interceptors. 

---

## PART 22 — ADMIN FEATURES

- **Excel Bulk Upload:** Admins upload `.xlsx` files. The backend parses rows (Product Name, Price, SKU, Category) and bulk inserts them.

---

## PART 23 — TESTING

- **xUnit & WebApplicationFactory:** Spins up an in-memory test server connected to a real test database (or in-memory DB) to run end-to-end integration tests (like Cart -> Checkout -> Order).
- **Number of tests:** Over 20 comprehensive integration tests covering the critical paths.

---

## PART 24 — CONFIGURATION

- **CORS:** The API explicitly configures Cross-Origin Resource Sharing to allow `http://localhost:5173` (React) to make requests to `https://localhost:52549` (API) and expose headers like `Idempotency-Key`.

---

## PART 25 — COMPLETE API REFERENCE

| Method | Endpoint | Auth | Role | Purpose |
|--------|----------|------|------|---------|
| POST | `/api/auth/login` | None | None | Authenticate user |
| POST | `/api/auth/register` | None | None | Register new user |
| GET | `/api/products` | None | None | List catalog |
| POST | `/api/products` | Yes | Admin | Create product |
| POST | `/api/products/{id}/image` | Yes | Admin | Upload image |
| DELETE| `/api/products/{id}` | Yes | Admin | Hard/Soft delete product |
| GET | `/api/cart` | Yes | Cust | View cart |
| POST | `/api/orders/checkout`| Yes | Cust | Place order |
| GET | `/api/orders` | Yes | Both | View order history |
| PUT | `/api/orders/{id}/status` | Yes | Admin | Update order status |

---

## PART 26 — COMPLETE FRONTEND ROUTE REFERENCE

| Route | Component | Access | Purpose |
|-------|-----------|--------|---------|
| `/` | Home | Public | Landing page |
| `/login` | Login | Public | Authentication |
| `/products`| Products | Public | Catalog |
| `/cart` | Cart | Auth | Shopping cart |
| `/checkout`| Checkout | Auth | Shipping & payment |
| `/admin/*` | AdminDashboard| Admin | Admin management views |

---

## PART 27 — COMPLETE USER FLOWS

**Product Deletion Flow (R11):**
Admin clicks Delete -> Backend `ProductService` intercepts -> Attempts to delete CartItems referencing the product -> Checks if OrderItems exist. If yes -> Sets `Status = Inactive`. If no -> Hard deletes from DB -> Triggers `IFileStorageService.DeleteFileAsync(ImageUrl)` -> Removes physical image.

---

## PART 28 — DESIGN PATTERNS & PRINCIPLES

- **Repository Pattern:** Abstracts DB access.
- **Dependency Injection (SOLID - D):** High-level modules don't depend on low-level modules, both depend on abstractions (Interfaces).
- **DTO Pattern:** Decouples domain entities from API contracts, preventing over-posting attacks and circular references.

---

## PART 29 — SECURITY

- **SQL Injection:** Prevented natively by EF Core parameterization.
- **Path Traversal:** File uploads generate random GUID filenames (`Guid.NewGuid().ToString() + ext`), ignoring the user's provided filename.
- **Mass Assignment:** Prevented by mapping requested fields to DTOs, never directly binding HTTP bodies to EF Core Entities.

---

## PART 30 — KNOWN LIMITATIONS

- **Microservices:** NOT IMPLEMENTED (Modular Monolith used).
- **Azure Deployment:** NOT IMPLEMENTED (Local IIS Express / Kestrel).
- **Real Payment Gateway:** PARTIAL (Mock payment service implemented).

---

## PART 31 — INTERVIEW QUESTIONS

1. **Why EF Core?**
   *Testing:* ORM knowledge.
   *Answer:* It boosts developer productivity with LINQ, manages migrations, and prevents SQL injection.
2. **What is Idempotency and why is it in checkout?**
   *Testing:* Distributed systems knowledge.
   *Answer:* Ensures a request processed multiple times yields the same result. Prevents double-charging if the user clicks "Submit" twice or the network drops.
3. **How does SignalR work here?**
   *Testing:* Real-time knowledge.
   *Answer:* WebSockets push notifications from the server to the specific connected client when an order status changes.

---

## PART 32 — EXPLAIN THIS PROJECT IN AN INTERVIEW

**1-minute explanation:**
"I built a complete E-Commerce platform using a modern tech stack: React on the frontend and an ASP.NET Core Web API on the backend, backed by SQL Server and EF Core. It features a full shopping cart and checkout experience with robust inventory management, idempotency for safe transactions, and real-time order updates via SignalR. I designed it using Clean Architecture to ensure separation of concerns, making the system highly testable and maintainable."

---

## PART 33 — WHY DID YOU CHOOSE THIS?

**Why modular monolith over microservices?**
"For a project of this scale, starting with microservices introduces premature complexity—network latency, distributed transactions, and deployment overhead. By organizing the codebase into strict layers (Domain, Application, Infrastructure), I achieve the same logical separation. If the platform scales massively, extracting a module (like Orders) into its own microservice is straightforward because the boundaries are already respected."

---

## PART 34 — DEBUGGING STORIES

**Checkout 400 Bad Request Bug:**
- *Symptom:* Checkout failed silently on the frontend with HTTP 400.
- *Root Cause:* The frontend React state managed the zip code as `shippingZipCode`, but the backend C# DTO required `ShippingPostalCode`. ASP.NET Core's automatic model validation rejected the request before hitting the controller.
- *Fix:* Aligned the payload mapping in `Checkout.jsx` to match the DTO exactly.
- *Lesson:* Always ensure API contracts (DTOs) strictly match the JSON payloads, and utilize proper error logging to catch automated validation rejections.

---

## PART 35 — FINAL CHEAT SHEET

- **Stack:** React, ASP.NET Core 8/10, EF Core, SQL Server, SignalR.
- **Architecture:** Clean Architecture (Domain -> Application -> Infrastructure -> API).
- **Core Entities:** Product, Inventory, Cart, Order, User.
- **Auth:** JWT stored in localStorage, Bearer token in Axios interceptor.
- **Key Features:** Idempotent checkout, transaction-wrapped inventory deductions, real-time SignalR toasts, physical file storage for images, soft-deletion for historical data integrity.
"""

with open('FINAL_PROJECT_STUDY_GUIDE.md', 'w', encoding='utf-8') as f:
    f.write(doc)
