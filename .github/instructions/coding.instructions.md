# Coding Instructions (repository-level)

Purpose: prescriptive rules for implementing code in this repository. These are enforced expectations — follow them when authoring production code.

1) Public surface & visibility
- Only expose `interface` types publicly from libraries. All concrete implementation classes should be `internal` by default.
- Use `InternalsVisibleTo("<Your.Project>.Tests")` to allow test projects access to internals when necessary.
- Public exceptions: DTOs, configuration POCOs, stateless helper utilities, constants and static helper classes may be public.

2) Async & Cancellation
- All public async APIs must accept a `CancellationToken` and propagate it to downstream calls. Tests must include cancellation scenarios where applicable.

3) Logging & Tracing
- Use structured logging and include a `CorrelationId` in request flows. Instrument OpenTelemetry traces and provide naming conventions for spans.
- Never log secrets or raw PII; redact or mask sensitive fields.

4) Error Handling
- Map domain exceptions to typed `ProblemDetails` responses at API boundaries. Include an `error_code` for programmatic handling.

5) Validation
- Validate inputs at the API boundary (FluentValidation recommended). Reject invalid requests explicitly with `400` and validation details.

6) Concurrency & Transactions
- Prefer optimistic concurrency (row-version) for high‑contention tables. Use transactions for atomic multi-step changes.

7) Security
- Follow OWASP Top 10: input validation, parameterized queries, secure headers, HTTPS/HSTS, and CORS policies.
- Store secrets in a vault (Key Vault / Vault) and never commit them.

8) Testing Requirements
- Unit tests for domain logic; include negative cases, cancellation and boundary tests.
- Integration tests for persistence and messaging; use testcontainers in CI or a docker-compose test profile.

9) Documentation & Comments
- Public types and important behaviors require XML doc comments. Keep high-level design notes in `docs/`.

Enforcement & Automation
- Add Roslyn analyzers or CI checks to warn when concrete implementation classes are public in library projects.
- CI should run format checks, analyzers, unit tests, and coverage gates as defined in CI instructions.
