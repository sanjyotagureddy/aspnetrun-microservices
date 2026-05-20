# Architecture (skill)

Purpose: concise architectural rules for automated agents; written from a senior-architect perspective.

Core patterns
- Vertical Slice Architecture (VSA): implement features as self-contained slices including request/response models, validation, handlers, and persistence abstractions.
- Clean Architecture layering: Domain (core), Application (use-cases), Infrastructure (implementations), API (composition/wiring).

When to apply CQRS
- Use CQRS when read and write concerns diverge substantially (different scaling, security, or data models).
- Keep simple CRUD in a single model; adopt CQRS per feature where justified.

Event-driven and messaging guidance
- Prefer event-driven integration for eventual consistency between modules (RabbitMQ + MassTransit). Ensure events are versioned and idempotent.
- Define event contracts in `BuildingBlocks.EventBus.Messages` and keep them backward compatible where possible.

Module extraction checklist (to convert to microservice)
1. Module owns its data store and schema migrations.
2. Exposes clear API/contract (commands/events) with backward compatibility strategy.
3. No forbidden dependencies on other APIs' internals.
4. CI covers the module, and observability metrics/logging are in place.

Operational concerns
- Add health checks per service and expose metrics for Prometheus or equivalent.
- Ensure request tracing (correlation id) flows through all services and logs.
