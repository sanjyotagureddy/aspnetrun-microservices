# Design Patterns Instructions

Purpose: prescriptive guidance and examples for applying common patterns in this repository.

1) MediatR & Vertical Slices
- Use MediatR handlers for commands/queries inside vertical slices. Keep handlers small and use pipeline behaviors for validation, logging, and caching.

2) CQRS
- Adopt CQRS only when read and write concerns diverge. Keep simple CRUD as a single model.

3) Transactional Outbox
- Persist outbox rows in the same DB transaction as domain writes. Implement a background publisher that marks rows published.
- Monitor outbox backlog and alert if it grows beyond thresholds.

4) Sagas & Idempotency
- Use Sagas for multi-step flows (checkout → payment → fulfillment). Prefer choreography for simple flows; use orchestration for complex, ordered flows.
- Enforce idempotency keys for state-changing HTTP commands and store keys with TTL.

5) Resilience & Policies
- Register central Polly policies for timeout, retry, circuit breaker and bulkhead. Inject policies via typed clients or decorators.

6) Caching
- Use cache-aside for catalog reads; invalidate caches explicitly on writes. Do not rely on in-memory caches across scaled instances.

7) Contract & Consumer Testing
- Publish consumer contracts (Pact) for public APIs. Run provider verification in CI before merging breaking changes.

Examples & Templates
- Add reusable implementations (BuildingBlocks) for outbox, idempotency middleware and saga starter. Reference these from services rather than duplicating logic.
