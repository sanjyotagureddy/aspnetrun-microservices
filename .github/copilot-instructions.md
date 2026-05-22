# Copilot / Contributor Instructions

Purpose
- Capture architecture, coding, testing and design guidance to keep the codebase consistent and maintainable.
- Intended for human contributors and automated assistants (Copilot/agents) to follow while authoring code, tests and CI.

Audience
- Contributors, reviewers, and automated coding assistants.

High-level architecture
- Follow Vertical Slice Architecture (VSA) + Clean Architecture principles: each feature is a vertical slice that owns its request/response, validators, handlers, DTOs and persistence abstractions.
- Separate concerns into layers: Domain (entities, value objects, domain logic), Application (use-cases/commands/queries, DTOs, MediatR handlers), Infrastructure (EF/Dapper, Repositories, gRPC clients), API (controllers, minimal wiring). Keep UI/gateways thin.
- Design as a modular monolith: modules (vertical slices) live in the same repo and solution but must be isolated behind interfaces and clear boundaries so modules can be extracted into microservices with minimal coupling.

Core principles (must be enforced)
- SOLID: single responsibility, open/closed via abstractions, Liskov-friendly inheritance, dependency inversion (use interfaces), composition over inheritance.
- Explicit boundaries: projects may only depend on lower-level projects as listed in the project dependency policy below. No circular references.
- Domain-centric models: domain entities are the source-of-truth; DTOs map explicitly to/from domain models at the application boundary.

Project / dependency rules
- Project types and examples:
  - `*.Domain` — domain entities, enums, value objects: no external deps (except shared building-blocks). No EF/ORM, no external services.
  - `*.Application` — use-cases, commands, queries, DTOs, MediatR handlers. Depends on `*.Domain` and `BuildingBlocks` only.
  - `*.Infrastructure` — persistence implementations, third-party SDKs, messaging clients. May depend on `*.Application` interfaces and `*.Domain` but expose only implementations.
  - `*.API` / Gateways — controllers, DI wiring, routing. Depends on `*.Application` interfaces.
  - `BuildingBlocks.*` — shared cross-cutting code (EventBus.Messages, Logging abstractions). Keep it minimal and generic.
- Strict rule: APIs/clients must depend on `Application` interfaces or `BuildingBlocks` only. Do not reference other service `*.API` projects or concrete infrastructure across modules.

Naming conventions
- Types and files: `PascalCase` for classes, interfaces, enums, and file names matching the main public type (e.g., `CreateOrderHandler.cs`).
- Interfaces: `I` prefix + PascalCase (e.g., `IOrderRepository`).
- Methods and parameters: `camelCase` for parameters, `PascalCase` for public methods.
- Namespaces: `[Company].[Product].[Service].[Layer]` or `[Solution].[Service].[Layer]` (be consistent per project). Keep namespaces matching folder structure.
- DTOs: suffix with `Dto` or `Request` / `Response` depending on usage. Commands/Queries named `CreateOrderCommand`, `GetOrderQuery`.

Coding guidelines
- Prefer small, focused classes and methods. Keep methods < 40 lines where practical.
- Prefer constructor injection; avoid the service locator pattern.
- Use `IOptions<T>` for configuration POCOs where appropriate.
- Avoid static mutable state; prefer scoped/transient DI lifetimes as appropriate.
- Use cancellation tokens on async public APIs/controllers and pass them through to downstream calls.
- Handle exceptions at boundaries; use typed results or domain exceptions internally and map to HTTP response codes in the API layer.

Design patterns and practices
- Use MediatR for implementing vertical slice handlers (commands/queries).
- Use Repository pattern sparingly — prefer thin repositories or explicit persistence services in `Infrastructure` bound by interfaces declared in `Application` or `Domain` if needed.
- Use Adapter/Facade patterns to isolate external SDKs and clients.
- Use Decorator pattern for cross-cutting concerns (caching, validation, retry) via pipeline behaviors/middleware.

Testing
- Unit testing framework: `xUnit`.
- Mocking: `Moq`.
- Assertions: `Shouldly`.
- Test project naming: `<Project>.Test` (e.g., `Ordering.API.Test`). Keep tests co-located in the `tests/` folder as current structure.
- Coverage requirement: target >= 90% code coverage for all production code. CI must fail builds that drop below the configured threshold.
- Use `coverlet.collector` or `coverlet.msbuild` + `reportgenerator` to generate coverage reports and badges.
- Tests must be fast and deterministic; avoid hitting external resources in unit tests — use mocks/fakes. Integration tests may use testcontainers or dedicated docker compose stacks and be marked/invoked separately.

CI / enforcement
- Add a CI workflow to restore, build, run unit tests, generate coverage and fail if coverage < 90%.
- Add CodeQL or SCA for security scanning (already present) and Dependabot for dependency updates.
- Integrate `dotnet format`, Roslyn analyzers, and a rule-set to treat certain analyzer warnings as errors. Prefer `Microsoft.CodeAnalysis.FxCopAnalyzers` and `dotnet/format` in pre-commit or CI.
- Add a pre-commit hook (or GitHub action) that runs `dotnet format --verify-no-changes` and prevents commits that break format rules.

Tooling recommendations
- `.editorconfig` with indentation, naming styles and file header policies.
- `Directory.Build.props` to centralize package versions and analyzer rules.
- Use `coverlet` + `reportgenerator` for coverage; enable codecov/coveralls or upload to CI artifacts.
- Add `git-secrets` or a scanner to block committing credentials.

PR checklist (required for all PRs)
- [ ] Code compiles and all unit tests pass locally.
- [ ] Coverage report produced and coverage not decreased below threshold.
- [ ] New behavior covered by unit tests. No unrelated test failures.
- [ ] No secrets or passwords in diffs.
- [ ] Naming and namespace conventions followed.
- [ ] Architecture rules respected — no forbidden project references.

Guidance for Copilot / Automated Assistants
- Always prefer solutions that respect the VSA + Clean Architecture rules in this file and the `docs/` pages.
- When suggesting code, include unit tests for new logic (xUnit + Moq + Shouldly).
- Do not introduce new project-level dependencies that break the declared project dependency rules.
- Prefer small, incremental changes and include a minimal migration plan if structural changes are proposed.

Instruction sources (compact)
- `.github/instructions/` — canonical repository instruction documents. Use these files as the source of truth for architecture, coding, CI, testing and project-dependency guidance.

Automated assistant usage
- Read the corresponding instruction file under `.github/instructions/` for quick, machine-friendly rules and then consult the `docs/` files for complete rationale and examples.
- You may now delete the legacy `.github/skills/` SKILL.md files — they were copied into VS Code prompts and the `.github/instructions/` directory; keep only `.github/instructions/` as the repo canonical source.
- When making changes that affect architecture, update the matching instruction file and propose an ADR in `docs/architectural-decisions.md`.

Onboarding and future enforcement
- Add CI enforcement and automated checks incrementally: formatting/analyzers → unit tests → coverage gate → secret scanning.
- Document any approved exceptions in `docs/architectural-decisions.md` (ADR) with rationale and owner.

Contact / Ownership
- Add maintainers or owners to `CODEOWNERS` for each service so PRs get correct reviews.

---
These instructions are a living document. Update with team agreement and record decisions in ADRs.
