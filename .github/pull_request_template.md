
# 🚀 Pull Request Summary

Provide a **clear and concise** summary of the changes and why they were made. Reference related issues or RFCs.

✅ Fixes: _(Issue #, if applicable)_

---

## 🛠 Type of Change

- [ ] 🐞 Bug Fix
- [ ] ✨ New Feature
- [ ] 📈 Enhancement
- [ ] 🔄 Refactor
- [ ] 💥 Breaking Change
- [ ] 🏗 Infrastructure / CI/CD
- [ ] 📖 Documentation
- [ ] 🎭 Tests (unit/integration/contract/perf)
- [ ] ⚖ Compliance / Security

---

## 📜 Commit Message Guidelines

🔹 Write descriptive commit messages — state **what** changed and **why**.
🔹 Use a structured format when possible (example):

- `MEARA-XX - feat(auth): Add JWT authentication [PROJ-123]`
- `fix(payment): Resolve race condition in transaction processing`

🔹 Keep commits focused and avoid mixing unrelated changes.

---

## ✅ Pre-Merge Checklist

### Build, Tests & Quality

- [ ] Branch targets the correct base and is up-to-date with `master`.
- [ ] Code builds locally: `dotnet build`.
- [ ] All unit tests pass locally: `dotnet test`.
- [ ] New or changed production code includes automated tests, or a documented rationale explains why not.
- [ ] Integration tests / contract tests updated and passing where relevant.
- [ ] Formatting: `dotnet format --verify-no-changes`.
- [ ] Static analyzers and linters: no new warnings treated as errors.
- [ ] Coverage: changes do not reduce unit test coverage below the configured threshold.
- [ ] Coverage report generated for impacted test projects and attached or summarized in this PR.

### Docs & API Contracts

- [ ] README or service README updated if behaviour or configuration changed.
- [ ] OpenAPI/Swagger updated for API changes and contract tests updated (Pact or equivalent).

### Database & Migrations

- [ ] Include migration scripts with a backward-compatible migration plan.
- [ ] Describe rollback steps and migration verification steps.

### Security & Compliance (required for payments/orders)

- [ ] No secrets or credentials in the diff.
- [ ] No raw card data or sensitive PII stored in logs or DB.
- [ ] Webhook signing and validation documented and implemented where applicable.
- [ ] Tag CODEOWNERS and request security review for changes affecting payments, orders, or customer data.

### Operational & Release

- [ ] If feature flag or gradual rollout required, include rollout plan.
- [ ] Update monitoring/alerts and SLOs if behavior affects metrics.

### Architecture and Governance

- [ ] Add/update ADR if this PR changes or adds architectural decisions.

---

## 🔬 How to verify locally

Provide commands and environment variables needed to verify the change locally. Use `dotnet user-secrets` for local secrets.

Example:

```bash
dotnet build src/Services/Catalog/Catalog.API/Catalog.API.csproj
dotnet test tests/Catalog/Catalog.API.Test/Catalog.API.Test.csproj
```

If there are integration tests requiring containers, document how to run them (testcontainers or `docker-compose.test.yml`).

---

## 🔒 Security & Compliance Checklist

- [ ] Confirmed no PII leakage in logs or telemetry.
- [ ] Confirmed no payment/card data is stored or logged.
- [ ] Confirmed required SCA/code scanning passes or findings are triaged.

---

## 🔍 Reviewer Notes

- Highlight non-obvious design decisions, tradeoffs, and any areas needing focused review.
- Suggest reviewers (e.g., `@org/payments-team`, `@org/ordering-team`).

---

By checking the boxes you confirm you have followed the repository `docs/` guidelines (architecture, coding, testing, security-and-compliance).
