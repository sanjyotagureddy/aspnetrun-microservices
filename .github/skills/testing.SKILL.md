# Testing (skill)

Purpose: machine-friendly testing policy and enforcement details.

Testing pyramid
- Unit tests: fast, deterministic, cover business logic (xUnit + Moq + Shouldly).
- Integration tests: exercise persistence and messaging using testcontainers or docker-compose in CI; mark these separately.
- End-to-end tests: cover critical user journeys; run less frequently.

Coverage and enforcement
- Target >= 90% for production code; run coverlet in CI and fail if below threshold.
- Use `coverlet.collector` and `reportgenerator` to produce HTML and Cobertura artifacts.

Recommended commands for CI
```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Isolation
- Unit tests must not touch external resources; use `Moq` or lightweight fakes.
- Integration tests may use ephemeral containers and should be gated from fast unit test runs.

