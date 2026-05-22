# CI Instructions

Purpose: prescriptive CI pipeline stages, failure modes and artifacts.

Pipeline stages
1. Format check: `dotnet format --verify-no-changes`.
2. Static analyzers: run Roslyn analyzers configured in `Directory.Build.props` and fail on configured warnings.
3. Build: `dotnet restore` + `dotnet build --no-restore`.
4. Unit tests + coverage: `dotnet test --collect:"XPlat Code Coverage"` and fail if coverage < 90%.
5. Integration tests (separate job): use testcontainers or `docker-compose.test.yml`.
6. Contract verification: run Pact/provider verification before merge.
7. Security scans: CodeQL, SCA, and secret scanning.

Artifacts
- Publish coverage reports (HTML + Cobertura) and test results as CI artifacts.

Enforcement
- CI must fail on forbidden project dependencies, uncovered security-critical code paths, or missing migrations when DB schema changes.
