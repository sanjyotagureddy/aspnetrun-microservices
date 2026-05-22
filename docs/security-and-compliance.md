# Security and Compliance (e‑commerce)

This document summarizes high‑priority security and compliance requirements for the e‑commerce system.

## PCI & Payments
- Do not store raw card data in any application or database. Use a PCI‑compliant payment gateway and tokenization.
- Handle webhooks securely: validate signatures, use replay protection, and restrict endpoints to known IP ranges where possible.
- Reconciliation: store gateway transaction IDs, idempotency keys, and reconciliation logs separately from sensitive data.

## PII / Data Privacy
- Classify PII (customer name, email, addresses, payment tokens) and apply encryption at rest and in transit.
- Implement data retention and deletion workflows (GDPR/CCPA): record consent, provide deletion endpoints and background purge jobs.

## Secrets and Configuration
- Use a vault (Azure Key Vault, HashiCorp Vault) for secrets in CI/CD and production. Do not commit secrets to repo.
- Developers use `dotnet user-secrets` for local dev and a secure vault for CI and production.

## Dependency & Vulnerability Management
- Run SCA (CodeQL or Snyk) in CI on every PR and on a schedule. Address high/critical findings promptly.
- Maintain an approved dependency policy and regular upgrade cadence for major security fixes.

## Application Security
- Follow OWASP Top 10 guidance: validate inputs, protect XSS/CSRF, use parameterized queries, and rate limit sensitive endpoints.
- Require HTTPS everywhere; HSTS enabled for production.
- Apply least-privilege IAM roles for services and databases.

## Logging & Monitoring
- Avoid logging secrets or PII. Mask or redact sensitive fields.
- Centralized logs (ELK/Datadog) and alerts for anomalies (spikes, error rates, payment failures).

## Incident Response & Auditing
- Maintain an incident response runbook for data breach and payment incidents: containment, notification, forensic capture.
- Enable auditing for admin actions and payment-related changes.

## Compliance Artifacts
- Keep PCI and privacy evidence (scans, policy documents, SOC reports) in a secured repo or compliance tool.

--
Add project-specific integration notes (gateway sandbox keys, webhook endpoints) to the service README files.
