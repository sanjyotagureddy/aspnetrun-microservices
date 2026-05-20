# Design Patterns (skill)

Purpose: succinct pattern guidance and reuse rules for implementers/automations.

Recommended patterns and when to use them
- MediatR (Mediator): use for decoupling request/handler logic inside vertical slices (commands/queries, pipeline behaviors for validation/caching).
- CQRS: adopt for complex modules where read/write models differ; separate read models for optimized queries.
- Saga / Orchestration: use for long-running business processes spanning multiple modules; prefer choreography (events) when possible.
- Adapter/Facade: wrap external SDKs (DB clients, third-party APIs) to isolate callers from SDK changes.
- Decorator / Pipeline behaviors: implement cross-cutting concerns (retry, caching, logging, validation) as decorators or MediatR pipeline behaviors.
- Circuit Breaker / Retry (Polly): wrap external calls with resilience policies; register policies centrally and inject them.

Reusability rules
- Keep pattern implementations infrastructure-agnostic: surface behavior through interfaces defined in `Application`.
- Package reusable behaviors into `BuildingBlocks.*` libraries and avoid leaking internal types.

