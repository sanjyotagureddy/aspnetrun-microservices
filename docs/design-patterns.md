# Design patterns and practices

Recommended patterns
- MediatR for vertical-slice handlers (commands/queries).
- Adapter/Facade: isolate external SDKs and clients behind interfaces.
- Decorator: for cross-cutting concerns via pipeline behaviors or middleware (caching, validation, retry).
- Repository: use thin repositories or explicit persistence services; declare interfaces in `Application` or `Domain`.

When to extract
- If a module grows its own infrastructure and team, extract it into a microservice only after creating clear boundaries and owning data.
