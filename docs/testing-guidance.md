# Testing Guidance (integration, contracts, performance)

This supplement extends `docs/testing.md` with patterns and examples for an e‑commerce system.

## Test Pyramid
- Unit tests: fast, pure, cover domain logic and validators.
- Integration tests: test service behavior with real infra (DB, message broker) using testcontainers in CI.
- Contract tests: consumer‑driven contracts (Pact) between services.
- End‑to‑end: smoke tests for critical flows (checkout) in a staging environment.

## Integration tests (practical)
- Use Testcontainers (Docker) to run a real database and broker during integration tests.
- Keep tests deterministic: seed known data and run migrations in test setup.

Example (dotnet + testcontainers):
1. Start a Mongo/SQL container for the test scope.
2. Run EF migrations or seed data.
3. Use `WebApplicationFactory` to host the API configured to the test containers.

## Contract testing
- Each public service exposes consumer contracts and providers verify them in CI.
- Run contract verification as part of CI pipeline before merging changes that modify public APIs.

## Performance & Load
- Add a CI stage for periodic load tests (k6) simulating peak traffic patterns (checkout spike, catalog browse).
- Capture metrics: P95 latency, errors/sec, throughput, and SLO breaches.

## Test organization & CI
- Separate slow/integration tests into a distinct test category and CI job.
- Keep coverage gate for unit tests; report combined coverage but enforce thresholds primarily on unit-level code.

## Local dev
- Provide a `docker-compose.test.yml` to run dependent services locally with sample data and a script to run integration tests.
