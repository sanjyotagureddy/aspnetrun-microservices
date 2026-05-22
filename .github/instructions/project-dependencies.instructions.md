# Project Dependency Instructions

Purpose: enforce concrete project reference rules and automated checks.

1) Allowed dependencies
- `*.Domain` may depend on `BuildingBlocks.*` and language libs only.
- `*.Application` may depend on `*.Domain` and `BuildingBlocks.*` only.
- `*.Infrastructure` may depend on `*.Application` and `*.Domain`.
- `*.API` may depend on `*.Application` and `BuildingBlocks.*` only.

2) Enforcement
- Add a Roslyn analyzer or MSBuild target that validates project references during CI. Fail CI on any forbidden reference.
- Maintain `ForbiddenDependencies.txt` showing disallowed pairs for quick audits.

3) Examples and migration
- If extracting a module, ensure it owns its DB, CI job, and API contracts before making it independently deployable.
