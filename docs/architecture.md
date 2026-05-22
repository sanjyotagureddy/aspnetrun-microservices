# Architecture

Summary
- Follow Vertical Slice Architecture (VSA) combined with Clean Architecture.

Guidelines
- Each feature is a vertical slice owning its request/response models, validators, handlers, DTOs and persistence abstractions.
- Layers: Domain (entities, value objects), Application (use-cases, DTOs, MediatR handlers), Infrastructure (persistence, SDKs, messaging), API (controllers, DI wiring).
- Design as a modular monolith: modules must be isolated behind interfaces and clear boundaries so extraction to microservices is straightforward.

Enforcement
- No domain logic in `Infrastructure` or `API` projects.
- Prefer interface-driven design and explicit mapping between DTOs and domain models.

## E‑commerce patterns
For e‑commerce specific flows (checkout, orders, inventory) prefer documented patterns:
- **Idempotency & Idempotency Keys** for all state‑changing HTTP commands to tolerate retries.
- **Transactional Outbox** for reliable event publishing when persisting orders/reservations.
- **Saga / Orchestration** for multi‑step flows that span services (checkout → payment → fulfillment).
- **Reservation / Hold** pattern for inventory to prevent oversell; short TTL holds with background release.

See `docs/checkout-and-orders.md` for full guidance and operational notes.
