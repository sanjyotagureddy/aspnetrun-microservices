# Testing Instructions

Purpose: prescriptive testing guidance, CI organization and example commands.

1) Test Pyramid and Categorization
- Unit tests: fast, deterministic, isolated from external resources. Use Moq/fakes.
- Integration tests: interact with real infra via testcontainers or docker-compose.test.yml. Mark them separately and run in a separate CI job.
- End-to-end: smoke tests for critical flows (checkout), run in staging.

2) Coverage
- Target >= 90% coverage for production code. Run `dotnet test --collect:"XPlat Code Coverage"` and use `reportgenerator` to produce artifacts.

3) Contract Testing
- Use Pact or similar for consumer-driven contract testing. Providers must verify contracts in CI.

4) Performance and Load
- Schedule periodic load tests (k6) for peak flows and measure P95/P99 latency and errors/sec.

5) Local Dev
- Provide `docker-compose.test.yml` to run DB and broker locally for integration tests.

6) Example CI Commands
```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```
