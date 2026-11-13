# ECommerce Project Constitution & Architecture Governance

**Document Version:** 1.0.0  
**Effective Date:** 2026-11-05  
**Governing Tool:** GitHub Speckit (`.specify/`)  
**Target Environment:** .NET 10, React 19, SQL Server, Ocelot Gateway, Podman  

---

## Preamble

This constitution establishes the engineering contract, architectural principles, coding standards, and operational conventions for the ECommerce microservices repository. Every specification in `specs/`, implementation plan, automated workflow, Copilot suggestion, and developer pull request must comply with these rules.

To maintain clarity, all rules are classified under two distinct categories:
- **[CURRENT IMPLEMENTATION]**: Practices, mechanisms, and structures that are currently active and enforced in the existing codebase.
- **[UC4 RECOMMENDATION]**: Target improvements, standards, and refinements to be progressively introduced throughout UC4 (OpenAPI verification, SDK generation, automated linting, security hardening, and Microsoft Foundry integration).

---

## 1. Architecture & Design Principles

### 1.1 Microservices Isolation
- **[CURRENT IMPLEMENTATION]** The system is partitioned into four independent business microservices (`UserService`, `ProductService`, `OrderService`, `PaymentService`) plus an edge gateway (`ApiGateway`) and a single-page application (`frontend`).
- **[CURRENT IMPLEMENTATION]** Services run in independent process spaces, possess dedicated dependency injection containers, and communicate only via HTTP REST or asynchronous event mechanisms. No service directly references another service's assemblies or DLLs.
- **[UC4 RECOMMENDATION]** Contract boundaries between services must be validated using exported OpenAPI specifications and automated schema compatibility checks before cross-service changes are merged.

### 1.2 Clean Architecture / Layer Separation
- **[CURRENT IMPLEMENTATION]** Each microservice strictly adheres to Clean Architecture with four distinct project layers:
  1. **Domain Layer (`*.Domain`):** Contains enterprise business entities, value objects, domain exceptions, enums, and repository interfaces. It has zero external dependencies and does not depend on EF Core or web frameworks.
  2. **Application Layer (`*.Application`):** Implements business workflows, command/query use cases, DTOs, service interfaces, mapping logic, and validation rules. It depends strictly inward on the Domain layer.
  3. **Infrastructure Layer (`*.Infrastructure`):** Implements EF Core `DbContext`, database entity configurations, repository implementations, inter-service HTTP clients, and external integrations. It depends inward on Application and Domain layers.
  4. **API Layer (`*.API`):** Acts as the HTTP entry point, containing controllers, middleware, background workers, and Swagger configuration. It wires dependencies via `DependencyInjection.cs` and exposes endpoints.
- **[CURRENT IMPLEMENTATION]** The dependency arrow points inward toward domain models:
  - `Domain` has zero project references across all services.
  - `Application` references only `Domain`.
  - `Infrastructure` references `Application` and `Domain`.
  - `API` references `Application` and `Infrastructure` to wire dependency injection in `Program.cs`. (Note: In `PaymentService.API`, an existing direct reference to `PaymentService.Domain` is present alongside Application and Infrastructure, while the other three services reference only Application and Infrastructure). Reverse dependencies from Domain/Application outward are strictly prohibited.

---

## 2. Data Management & Boundaries

### 2.1 Database Ownership
- **[CURRENT IMPLEMENTATION]** Database-per-service ownership is absolute. Each microservice connects exclusively to its own dedicated database:
  - `UserService` -> `UserServiceDb`
  - `ProductService` -> `ProductServiceDb`
  - `OrderService` -> `OrderServiceDb`
  - `PaymentService` -> `PaymentServiceDb`
- **[CURRENT IMPLEMENTATION]** Cross-database queries, cross-database joins, and cross-database foreign keys are strictly prohibited at both the database and application levels.
- **[CURRENT IMPLEMENTATION]** Migrations are managed independently within each service's Infrastructure project using EF Core Migrations (`Add-Migration` / `dotnet ef migrations add`).

### 2.2 Eventual Consistency & Outbox Pattern
- **[CURRENT IMPLEMENTATION]** Distributed transactions (2PC) are forbidden across service boundaries. State transitions spanning multiple services use eventual consistency.
- **[CURRENT IMPLEMENTATION]** Reliable message dispatch is implemented via the Transactional Outbox Pattern:
  - Events are saved to an `OutboxMessages` table in the same database transaction as business state changes.
  - Dedicated background services (`OutboxProcessorBackgroundService` in OrderService, `PaymentOutboxProcessor` in PaymentService) poll and dispatch pending messages.
- **[CURRENT IMPLEMENTATION]** Compensating actions are executed when downstream operations fail (e.g., releasing reserved inventory upon payment failure or order cancellation).
- **[UC4 RECOMMENDATION]** Transition from polling background workers to change-data-capture (CDC) or a hardened message broker if system throughput requirements exceed SQL-based outbox table capacity.

---

## 3. API Design & OpenAPI Standards

### 3.1 RESTful URL Conventions
- **[CURRENT IMPLEMENTATION]** Resources are named using plural nouns in lower-case or kebab-case (e.g., `/api/products`, `/api/orders`, `/api/users/profile`).
- **[CURRENT IMPLEMENTATION]** Standard HTTP verbs convey operations:
  - `GET`: Read resource without side-effects.
  - `POST`: Create resource or execute non-idempotent action.
  - `PUT`: Full update or state replacement.
  - `DELETE`: Remove resource.
- **[CURRENT IMPLEMENTATION]** HTTP status codes must accurately reflect the outcome:
  - `200 OK`: Successful retrieval or synchronous state transition with response payload.
  - `201 Created`: Successful creation, returning resource and `Location` header where applicable.
  - `204 NoContent`: Successful operation with empty response body.
  - `400 BadRequest`: Validation failure or business invariant violation.
  - `401 Unauthorized`: Missing or invalid authentication token / key.
  - `403 Forbidden`: Authenticated user lacks required role/permission.
  - `404 NotFound`: Requested resource does not exist.
  - `409 Conflict`: Concurrent mutation conflict or duplicate idempotency submission.

### 3.2 OpenAPI as First-Class Contract
- **[CURRENT IMPLEMENTATION]** OpenAPI specifications are generated dynamically using Swashbuckle and serve as the single source of truth for service capabilities.
- **[CURRENT IMPLEMENTATION]** XML documentation comments are enabled in `.csproj` files (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`) and included in SwaggerGen.
- **[CURRENT IMPLEMENTATION]** Every controller action must include `[ProducesResponseType]` for all potential status codes and schemas.
- **[UC4 RECOMMENDATION]** Generate and commit static OpenAPI JSON/YAML specifications into version control (`specs/openapi/`) to enable automated schema drift detection and pre-commit linting (Spectral).

---

## 4. Authentication, Authorization & Security

### 4.1 User-Facing Authentication (JWT Bearer)
- **[CURRENT IMPLEMENTATION]** User authentication uses JSON Web Tokens (JWT) issued by `UserService`.
- **[CURRENT IMPLEMENTATION]** Protected endpoints require a valid `Bearer <token>` HTTP `Authorization` header.
- **[CURRENT IMPLEMENTATION]** Operation-level security metadata is applied via `AuthorizeCheckOperationFilter`:
  - Endpoints with `[Authorize]` require JWT Bearer.
  - Endpoints with `[AllowAnonymous]` or without authorization attributes are documented as public.
  - Internal service endpoints are strictly excluded from JWT Bearer requirements.
- **[CURRENT IMPLEMENTATION]** Role-based authorization is enforced via `[Authorize(Roles = "Admin")]` for administrative actions (e.g., product creation, inventory adjustments).

### 4.2 User Identity Derivation
- **[CURRENT IMPLEMENTATION]** Authenticated actions must NEVER trust user identifiers passed in request query parameters or request bodies when the user's identity is required.
- **[CURRENT IMPLEMENTATION]** Identity must be derived securely from the authenticated token claims: `User.FindFirstValue(ClaimTypes.NameIdentifier)`.

### 4.3 Inter-Service Authentication
- **[CURRENT IMPLEMENTATION]** Private inter-service endpoints (e.g., inventory reservation in `ProductService`, payment notification in `OrderService`, refund initiation in `PaymentService`) are authenticated via the `X-Internal-Service-Key` header.
- **[CURRENT IMPLEMENTATION]** Internal endpoints are documented in OpenAPI using `InternalServiceHeaderOperationFilter` and must not be marked with JWT Bearer security.
- **[UC4 RECOMMENDATION]** Centralize internal service key validation using a shared middleware or filter attribute, replacing manual `Request.Headers["X-Internal-Service-Key"]` comparisons in controllers.
- **[UC4 RECOMMENDATION]** Store internal service keys exclusively in environment variables or Azure Key Vault rather than `appsettings.json`.

### 4.4 Data Protection & Secret Hygiene
- **[CURRENT IMPLEMENTATION]** Passwords must be hashed with strong salt algorithms (e.g., BCrypt / PBKDF2) before persistence.
- **[CURRENT IMPLEMENTATION]** Tokens, secret keys, and passwords must never be logged or returned in API response bodies.
- **[UC4 RECOMMENDATION]** Implement pre-commit secret scanning to prevent accidental committing of keys.

---

## 5. Reliability, Concurrency & Resilience

### 5.1 Idempotency Pattern
- **[CURRENT IMPLEMENTATION]** Non-idempotent mutation operations that involve financial or transactional risk require an `Idempotency-Key` HTTP header:
  - `OrderService`: `POST /api/order` (`CreateOrder` in `OrderController`) enforces `Idempotency-Key` and returns the cached order (HTTP 201) for repeated submissions per UC3 specification.
  - `PaymentService`: `POST /api/payments` (`CreatePayment` in `PaymentsController`) enforces `Idempotency-Key` and returns HTTP 409 on concurrent in-flight attempts.
  - `PaymentService`: `POST /api/payments/{id}/refund` (`CompleteRefund` in `PaymentsController`) enforces `Idempotency-Key` to guarantee refund deduplication.

### 5.2 Optimistic Concurrency Control
- **[CURRENT IMPLEMENTATION]** Entities prone to concurrent updates (e.g., `Order`) feature a `RowVersion` (`byte[]`) concurrency token.
- **[CURRENT IMPLEMENTATION]** Concurrency violations must be caught as `DbUpdateConcurrencyException` and translated to meaningful client feedback.

### 5.3 Error Handling & Diagnostics
- **[CURRENT IMPLEMENTATION]** Unhandled exceptions are intercepted by centralized `ExceptionHandlingMiddleware` across all microservices, ensuring internal stack traces are never exposed in production.
- **[CURRENT IMPLEMENTATION]** Health check endpoints (`/health`) are exposed by each service, monitoring database connectivity via `AddDbContextCheck`.
- **[UC4 RECOMMENDATION]** Standardize all error responses to conform strictly to RFC 7807 (`application/problem+json`).

---

## 6. Evolution, Versioning & Compatibility

### 6.1 API Versioning
- **[CURRENT IMPLEMENTATION]** APIs currently use unversioned URL routes (`/api/[controller]`, e.g., `/api/products`, `/api/order`). While Swashbuckle publishes documentation under document identifier `v1` (`/swagger/v1/swagger.json`), there is currently no URL path version segment (such as `/api/v1/...`).
- **[UC4 RECOMMENDATION]** Any breaking changes to request or response schemas must introduce an explicit version increment (e.g., `/api/v2/...` or query parameter versioning) to maintain backward compatibility for existing mobile or web clients.

### 6.2 Backward Compatibility Rules
- **[UC4 RECOMMENDATION]** Field additions to response DTOs are considered non-breaking.
- **[UC4 RECOMMENDATION]** Field removals, renaming, or changing types in request/response DTOs are breaking changes and require a deprecation window and major version increment.
- **[UC4 RECOMMENDATION]** Database migrations must be backward-compatible: new columns must be nullable or provide default values so older running code remains functional during rolling deployments.

---

## 7. Quality, Testing & Verification

### 7.1 Testing Standards
- **[CURRENT IMPLEMENTATION]** Unit tests verify domain logic, entities, and application services without external dependencies.
- **[CURRENT IMPLEMENTATION]** Integration tests use ASP.NET Core `WebApplicationFactory` to verify HTTP endpoints, middleware pipelines, and database interactions.
- **[UC4 RECOMMENDATION]** Introduce automated contract tests validating runtime responses against OpenAPI schemas.
- **[UC4 RECOMMENDATION]** Require test coverage gates for all new application services and domain models.

### 7.2 Verification Gates
- Every code change must satisfy:
  1. **Build Gate:** `dotnet build ECommerce.sln -m:1 --no-restore` compiles with 0 errors.
  2. **Frontend Build Gate:** `npm run build --prefix frontend` compiles with 0 errors.
  3. **Documentation Gate:** All modified public endpoints and DTOs have complete XML documentation and `[ProducesResponseType]` annotations.
  4. **Security Gate:** Public endpoints require no token; protected endpoints enforce authentication; internal endpoints validate internal keys.

---

## 8. Operational Hygiene & Refactoring Boundaries

### 8.1 Containerization & Infrastructure
- **[CURRENT IMPLEMENTATION]** Services and supporting infrastructure (SQL Server, Ocelot Gateway, React Frontend) are containerized using Podman and configured via `podman-compose.yml` (with individual service `Containerfile` definitions).
- **[CURRENT IMPLEMENTATION]** Configuration is separated by environment (`appsettings.json` vs `appsettings.Development.json` vs environment variables).
- **[CURRENT IMPLEMENTATION]** Gateway routing is dual-configured for host networks (`ocelot.json`) and container networks (`ocelot.docker.json`).

### 8.2 Scope Discipline & Refactoring Boundaries
- **[CURRENT IMPLEMENTATION]** **Zero unapproved refactoring rule:** Developers and AI assistants must never refactor unrelated code, modify functioning business logic, or alter working configurations outside the authorized task scope.
- **[CURRENT IMPLEMENTATION]** Cleanups, optimizations, and security patches must be tracked as distinct tasks or specifications and approved before implementation.

