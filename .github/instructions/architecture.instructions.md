# Architecture Instructions

Purpose: prescriptive architectural rules and the e‑commerce specific patterns to follow.

1) Layering & Vertical Slices
- Implement features as vertical slices: request/response models, validation, handlers, and persistence abstractions.
- Enforce project dependency rules: Domain <- Application <- Infrastructure <- API.

2) E‑commerce Patterns
- Idempotency: require `Idempotency-Key` for checkout/payment endpoints.
- Transactional Outbox: co-locate outbox writes with domain transactions.
- Reservation/hold pattern: implement inventory holds with TTL and automated release.

3) Messaging & Events
- Define event contracts in `BuildingBlocks.EventBus.Messages` and maintain backward compatibility.

4) Operational Requirements
- Health checks, metrics (Prometheus), correlation ids and tracing (OpenTelemetry) must be implemented per service.
