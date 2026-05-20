# CI (skill)

Purpose: compact CI rules for automated enforcement and pipeline authors.

Pipeline stages (recommended)
1. Format check: `dotnet format --verify-no-changes` (fail the build if formatting differs).
2. Static analysis: run Roslyn analyzers and treat configured warnings as failures.
3. Build: `dotnet restore` + `dotnet build --no-restore`.
4. Unit tests + coverage: `dotnet test --collect:"XPlat Code Coverage"` and fail if coverage < 90%.
5. Integration tests (optional): run tests that require containers using docker-compose or testcontainers.
6. Security scans: CodeQL / SCA and secret scanning.

Artifacts
- Publish coverage HTML and Cobertura artifacts for visualization and coverage gates.

Example assurances
- Enforce dependency rules by running the project reference analyzer during the build.
- Use Dependabot for dependency updates and automatically run tests on PRs.

