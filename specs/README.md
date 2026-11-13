# System Specifications Directory

This directory contains formal feature and architectural specifications for the ECommerce Microservices System, governed by GitHub Speckit and our project constitution (`.specify/memory/constitution.md`).

## Specification Lifecycle

1. **Draft:** Created using `.specify/templates/spec-template.md`. Initial proposal undergoing design review.
2. **In Review:** API contracts, database models, security requirements, and failure modes reviewed with team.
3. **Approved:** Contract frozen; implementation plan (`plan-template.md`) and task breakdown (`tasks-template.md`) generated.
4. **Implemented:** Code, tests, and documentation delivered and verified against `checklist-template.md`.
5. **Deprecated:** Replaced or retired by a subsequent specification.

## Naming Conventions

Specifications are stored with the naming pattern:
`specs/{id}-{title-kebab-case}.md`

Examples:
- `specs/001-openapi-standardization.md`
- `specs/002-order-checkout-idempotency.md`
- `specs/003-inventory-reservation-protocol.md`

## Active Specifications Index

| Spec ID | Title | Owning Service(s) | Status | Last Updated |
| :--- | :--- | :--- | :--- | :--- |
| *Pending Step 3B* | *OpenAPI Specification Export & Catalog* | All Services | Planned (Step 3B) | 2026-11-05 |

