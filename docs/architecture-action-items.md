<!-- Auto-generated action items from repo architecture review -->
# Architecture & Code Action Items

Last updated: 2026-05-24

This file tracks the architectural and code-quality action items discovered during repository analysis. Use the checkboxes to mark progress, add `Owner:` and `Due:` as needed, and update notes as you make changes.

## How to use
- Toggle the checkbox when an item is completed.
- Add `Owner:` and `Due:` inline under the item when assigned.
- Keep notes concise and link to PRs when available.

---

## Action Items (prioritized)

- [ ] **(High)** Refactor repositories to avoid auto-committing inside repository methods
  - Files: `src/Services/Ordering/Ordering.Infrastructure/Repositories/RepositoryBase.cs`
  - Notes: Introduce a `IUnitOfWork` (or `CommitAsync`) and make repo methods non-committing to allow transactional composition.
  - Owner: 
  - Due:

- [ ] **(High)** Propagate `CancellationToken` throughout async paths
  - Files: repository interfaces and handlers across `src/Services/*/*` (example: `RepositoryBase`, handlers in `Ordering.Application`)
  - Notes: Add `CancellationToken` parameters to repo methods and pass tokens from controllers/handlers.
  - Owner:
  - Due:

- [ ] **(High)** Replace hard-coded audit user in `OrderContext` with request-context enrichment
  - Files: `src/Services/Ordering/Ordering.Infrastructure/Persistence/OrderContext.cs`, `src/SharedKernel/Context/*`
  - Notes: Use `IRequestContextEnricher` or `RequestContext` to set `CreatedBy`/`LastModifiedBy` and avoid literals.
  - Owner:
  - Due:

- [ ] **(High)** Remove hard-coded email recipient in checkout handler
  - Files: `src/Services/Ordering/Ordering.Application/Features/Orders/Commands/CheckoutOrder/CheckoutOrderCommandHandler.cs`
  - Notes: Use customer/order data or configuration; consider queuing email send as background job and add retry.
  - Owner:
  - Due:

- [ ] **(High)** Ensure reliable publish-after-commit messaging (Outbox or transactional outbox)
  - Files: handlers that publish events (Ordering checkout flow) and MassTransit config in `Ordering.API`/`Infrastructure`
  - Notes: Prevent message loss by writing outbox records within DB transaction and publishing after commit, or use MassTransit-supported patterns.
  - Owner:
  - Due:

- [ ] **(High)** Implement transactional outbox (skeleton exists) and background publisher
  - Files: `src/BuildingBlocks/Outbox/TransactionalOutbox.cs` (skeleton)
  - Notes: A skeleton exists under `BuildingBlocks/Outbox` but it's conceptual. Implement DB-specific persistence, background publisher, monitoring and idempotency for publish. Ensure co-located outbox writes in the same DB transaction.
  - Owner:
  - Due:

- [ ] **(Medium)** Reduce layering coupling: avoid API -> Infrastructure direct project references
  - Files: `*.API` project references (e.g., `Ordering.API` csproj)
  - Notes: Move registration helpers to a `BuildingBlocks` registration project or create a small `Infrastructure.Registration` project exposing only DI helpers.
  - Owner:
  - Due:

- [ ] **(Medium)** Add unit & integration tests for critical flows
  - Files: `tests/*` and new tests covering handlers, repo error handling, mapping profiles
  - Notes: Add tests for validation behavior, mapping correctness, and happy/failure paths of `CheckoutOrder` and `GetOrdersList`.
  - Owner:
  - Due:

- [ ] **(Medium)** Ensure correlation ID and request-context propagation in logs and audits
  - Files: `src/SharedKernel/Middleware/*`, logging configuration in APIs
  - Notes: Logs should include correlation IDs; request context should be consistently enriched.
  - Owner:
  - Due:

- [ ] **(Low)** Add `CODEOWNERS`, `CONTRIBUTING.md`, and `CODE_OF_CONDUCT.md`
  - Files: repository root (new files)
  - Notes: Improves PR routing and contributor guidance.
  - Owner:
  - Due:

- [ ] **(Low)** Secrets & configuration audit
  - Files: all source; check for hard-coded credentials and user secrets usage
  - Notes: Ensure no credentials are committed; use managed secrets / CI secrets per docs.
  - Owner:
  - Due:

- [ ] **(Low)** Verify coverage gating in Azure pipeline and README badges
  - Files: `.azure/pipeline.yml`, `README.md`
  - Notes: Pipeline produces coverage artifacts; confirm threshold enforcement and add badges if desired.
  - Owner:
  - Due:

---

If you want, I can pick the top-priority item and implement a focused PR (example: add `IUnitOfWork` + refactor `RepositoryBase` and update `CheckoutOrderCommandHandler`).

## Messaging, Resilience and Security Gaps (explicit)

- **Outbox:** skeleton at `src/BuildingBlocks/Outbox/TransactionalOutbox.cs` but no production implementation. Required for reliable publish-after-commit.
- **Messaging contracts/versioning:** `BuildingBlocks/EventBus.Messages` contains shared messages; ensure semantic versioning and backward-compatible changes.
- **Resilience (Polly):** README/docs mention `Polly` but the codebase has no registered Polly policies or policy registry. Add central policy registration and use typed `HttpClient` with policies or decorator patterns.
- **Retry & common HTTP clients:** No centralized retry/decorator for inter-service HTTP/gRPC calls. Add typed clients with retry/circuit-breaker and test failure modes.
- **Service-to-service auth & RBAC:** No `AddAuthentication`/`AddAuthorization` or `[Authorize]` usage detected. Implement JWT/OAuth Bearer auth at API gateway and enforce RBAC/policies where needed.
- **Outbox + Saga coordination:** Sagas are referenced in docs but no clear saga coordinator/outbox integration is present; add saga starter and monitoring.

---
