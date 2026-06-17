# Cart Implementation Checklist

Track delivery for cart foundation, anonymous + authenticated ownership, Redis persistence, and inventory-safe checkout.

## Status legend

- [ ] Not started
- [x] Completed

## Phase 0: ADR and Domain Alignment

- [ ] Review and approve `ADR-0001-cart-and-inventory-reservation-strategy.md`.
- [ ] Review and baseline `Website-Functional-Scenario-Catalog.md`.
- [ ] Confirm cart ownership rules (`userId` and `anonymousCartId`).
- [ ] Confirm anonymous-to-authenticated merge behavior.
- [ ] Confirm reservation lifecycle (`reserve`, `release`, `commit`).
- [ ] Confirm idempotency policy and retention.

## Phase 0.5: Auth Service Foundation (Parallel Track)

- [x] Track execution using `../../Identity/Auth.Api/docs/Auth-Service-Implementation-Checklist.md`.
- [x] Define authentication architecture (OIDC provider choice, token format, issuer, audience).
- [x] Approve `../../Identity/Auth.Api/docs/Auth-v1-claims-scopes-policy-matrix.md` as the implementation baseline.
- [x] Implement exact v1 decision baseline from `../../Identity/Auth.Api/docs/Auth-v1-claims-scopes-policy-matrix.md` section "0. Decision Baseline (Approved for v1)".
- [x] Create Auth service boundaries (register, login, refresh, revoke, profile claims).
- [x] Define token claims contract required by cart/order (`sub`, tenant, roles/scopes).
- [ ] Define anonymous-to-authenticated trust flow for cart merge trigger.
- [x] Define tenant identity contract (`tenant_id` claim source of truth and validation rules).
- [ ] Publish Auth service readiness criteria for cart authenticated endpoints.

## Phase 0.6: Multi-Tenant Authorization Foundation (Parallel Track)

- [ ] Define tenant isolation model (shared DB with tenant discriminator vs schema/database per tenant).
- [ ] Define RBAC for tenant roles (`tenant_admin`, `catalog_manager`, `buyer`).
- [ ] Require tenant-scoped authorization policy for product write endpoints.
- [ ] Define cross-tenant access policy (default deny; explicit platform-admin exception only).
- [ ] Add tenant ownership checks in product read/write query paths.
- [ ] Add audit requirements for tenant-sensitive mutations.

## Sequencing Rule

- [ ] Start anonymous cart and Redis foundation first (Phases 1 through 3).
- [ ] Build Auth service in parallel (Phase 0.5).
- [ ] Build tenant isolation controls in parallel (Phase 0.6).
- [ ] Start authenticated cart features (Phase 4+) only after Auth readiness criteria are met.
- [ ] Start tenant-restricted product writes only after Phase 0.6 policy and enforcement are complete.
- [ ] Do not block guest cart launch on full Auth service completion.

## Phase 1: Cart API Foundation

- [ ] Replace scaffold endpoint with feature-based route groups.
- [ ] Add cart contracts (request/response DTOs) for get/add/update/remove.
- [ ] Add validators for product IDs and quantity boundaries.
- [ ] Add MediatR handlers for all cart mutations/queries.
- [ ] Add ProblemDetails + exception handling alignment.

## Phase 2: Redis Integration

- [ ] Add Redis resource in AppHost and reference it from `cart-api`.
- [ ] Configure Cart API Redis client with health checks.
- [ ] Implement Redis cart repository keyed by owner mode.
- [ ] Add optimistic concurrency with cart version/etag.
- [ ] Implement sliding TTL refresh policy by owner mode.

## Phase 3: Anonymous Cart Support

- [ ] Issue secure anonymous cart cookie for guest users.
- [ ] Create/resolve anonymous cart context middleware/filter.
- [ ] Enforce anonymous cart mutation rate limits.
- [ ] Enforce max item count and per-item quantity limits.
- [ ] Add telemetry for anonymous cart creation and updates.

## Phase 4: Authenticated Cart Support

- [ ] Resolve authenticated cart owner from identity claims.
- [ ] Add authenticated cart retrieval and mutation endpoints.
- [ ] Enforce ownership and access checks.
- [ ] Add telemetry for authenticated cart operations.

## Phase 5: Cart Merge on Sign-In

- [ ] Implement merge command (anonymous -> authenticated).
- [ ] Define and implement quantity conflict rule.
- [ ] Apply post-merge stock hint validation.
- [ ] Ensure merge is idempotent and auditable.
- [ ] Delete source anonymous cart after successful merge.

## Phase 6: Inventory Reservation Capability

- [ ] Add inventory API contract for reserve line items atomically.
- [ ] Add inventory API contract for release reservation.
- [ ] Add inventory API contract for commit reservation.
- [ ] Persist reservation state with expiration and cleanup.
- [ ] Enforce atomic insufficient-stock rejection under concurrency.

## Phase 7: Checkout Orchestration

- [ ] Add checkout endpoint/command using idempotency key.
- [ ] Reserve inventory for all line items before payment authorization.
- [ ] Create compensation flow (release on payment/order failure).
- [ ] Commit reservation only after order success.
- [ ] Clear or lock cart after successful checkout.

## Phase 8: Reliability and Observability

- [ ] Add structured logs for cart and reservation operations.
- [ ] Add metrics for reservation success/failure/conflicts/timeouts.
- [ ] Add traces across cart -> inventory -> order flow.
- [ ] Add alerts for reservation leak and high conflict rate.
- [ ] Add operational runbook for stuck reservations.

## Phase 9: Test and Quality Gates

- [ ] Unit tests for cart domain invariants and merge logic.
- [ ] Integration tests for Redis cart persistence and TTL behavior.
- [ ] Integration tests for reserve/release/commit lifecycle.
- [ ] Concurrency tests for oversell prevention (50 + 60 against 100).
- [ ] Idempotency tests for duplicate checkout requests.
- [ ] Coverage report for changed projects with non-regression in changed areas.
- [ ] Map every critical scenario from `Website-Functional-Scenario-Catalog.md` to automated tests.

## Scenario Acceptance Checklist

- [ ] Anonymous user can add/update/remove items without registration.
- [ ] Authenticated user cart persists across sessions.
- [ ] Anonymous cart merges correctly on sign-in.
- [ ] Two concurrent checkout attempts cannot oversell stock.
- [ ] Reservation expires and stock returns when checkout is abandoned.
- [ ] Payment failure releases reservation safely.
- [ ] Duplicate checkout request returns idempotent outcome.

## Tracking

- Owner: Cart + Inventory service teams
- Review cadence: Weekly
- Source ADR: `ADR-0001-cart-and-inventory-reservation-strategy.md`
- Scenario catalog: `Website-Functional-Scenario-Catalog.md`
