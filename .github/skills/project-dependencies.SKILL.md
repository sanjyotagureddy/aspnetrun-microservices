# Project Dependencies (skill)

Purpose: machine-readable rules for enforcing project boundaries in the modular-monolith.

Core rules (one-way dependency graph)
- Layer ordering: `Domain` <- `Application` <- `Infrastructure` <- `API`/Gateway.
- `BuildingBlocks.*` are shared lower-level libraries allowed to be referenced by any layer.

Concrete constraints
- `*.Domain`:
	- Contains entities, value objects, domain services, domain exceptions and enums.
	- MUST NOT depend on `Application`, `Infrastructure`, or `API` projects.
	- Allowed deps: `BuildingBlocks.*`, basic language libs.
- `*.Application`:
	- Contains DTOs, commands/queries, use-cases, MediatR handlers, interfaces for repositories/services.
	- May depend on `*.Domain` and `BuildingBlocks.*` only.
- `*.Infrastructure`:
	- Implementation of repositories, EF/Dapper, messaging adapters, gRPC/HTTP clients, third-party SDKs.
	- May depend on `*.Application` interfaces and `*.Domain` but must not expose infra types to upper layers.
- `*.API` / Gateways:
	- Controllers, minimal mapping/DTO translation, DI wiring and endpoint contracts.
	- May depend on `*.Application` interfaces and `BuildingBlocks.*` only.

Enforcement & automation
- Implement a repository analyzer (Roslyn or custom script) that validates project references and fails CI if violations exist.
- Add `ForbiddenDependencies.txt` or MSBuild targets to the build to catch accidental references early.

Examples
- Allowed: `Ordering.Application` -> `Ordering.Domain` (OK)
- Forbidden: `Ordering.API` -> `Catalog.API` (NOT OK)

