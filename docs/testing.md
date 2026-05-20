# Testing

Frameworks
- Unit tests: `xUnit`.
- Mocking: `Moq`.
- Assertions: `Shouldly`.

Policy
- Target >= 90% code coverage for production code. CI must enforce coverage gate.
- Fast, deterministic unit tests using mocks/fakes; integration tests run separately with docker/testcontainers.

Tooling
- Use `coverlet` + `reportgenerator` to produce coverage reports and artifacts.
