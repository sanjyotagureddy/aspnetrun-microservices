# Project dependency rules

Allowed project types
- `*.Domain` — domain entities, enums, value objects: no external deps (except shared building-blocks).
- `*.Application` — use-cases, commands, queries, DTOs, MediatR handlers. Depends on `*.Domain` and `BuildingBlocks` only.
- `*.Infrastructure` — persistence implementations, third-party SDKs, messaging clients. May depend on `*.Application` interfaces and `*.Domain` but expose only implementations.
- `*.API` / Gateways — controllers, DI wiring, routing. Depends on `*.Application` interfaces.
- `BuildingBlocks.*` — shared cross-cutting code.

Strict rules
- No circular references.
- No `*.API` depending on other `*.API` projects.
- Public interfaces for interactions; implementations live in `Infrastructure`.
