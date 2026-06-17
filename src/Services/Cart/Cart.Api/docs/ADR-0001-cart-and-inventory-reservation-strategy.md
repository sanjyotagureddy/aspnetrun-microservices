# ADR-0001: Cart and Inventory Reservation Strategy (Authenticated + Anonymous)

## Status
Proposed

## Date
2026-06-13

## Tracking

- Implementation checklist: [Cart-Implementation-Checklist.md](Cart-Implementation-Checklist.md)
- Functional scenario catalog: [Website-Functional-Scenario-Catalog.md](Website-Functional-Scenario-Catalog.md)

## Context
The platform needs to support both:

- Authenticated users with persistent carts across sessions/devices.
- Anonymous users who can add items without registration.

The system must also prevent oversell under concurrent checkouts.

Example risk:

- Available stock = 100.
- User A attempts checkout with quantity 50.
- User B attempts checkout with quantity 60.
- Without atomic reservation/commit semantics, both can pass pre-check and oversell.

Current state highlights:

- Cart API is currently scaffold-level only.
- Inventory supports initialize/read operations but not reserve/release/commit semantics.
- Cart runtime does not yet use Redis.

## Decision
Adopt a dual cart model (authenticated + anonymous) with Redis-backed cart state and inventory hard reservation at checkout.

### 1. Cart identity model
Support exactly two cart owner modes:

- `AuthenticatedCartOwner`: keyed by `userId`.
- `AnonymousCartOwner`: keyed by `anonymousCartId` (secure random opaque ID held in httpOnly cookie).

Rules:

- Anonymous users can create and update cart without account registration.
- If a user signs in and an anonymous cart exists, run merge flow.
- A cart belongs to one owner key at a time.

### 2. Anonymous-to-authenticated merge policy
Use deterministic merge rules at sign-in:

- Merge source: anonymous cart.
- Merge destination: authenticated cart for `userId`.
- If the same product exists in both carts, sum quantities and clamp to max per-item limit.
- Re-run lightweight stock hint validation after merge (non-blocking warning).
- Delete anonymous cart after successful merge and issue a new cart version/etag.

### 3. Redis as cart system of record
Use Redis for cart aggregate persistence.

Key model:

- `cart:user:{userId}` for authenticated carts.
- `cart:anon:{anonymousCartId}` for anonymous carts.

TTL policy:

- Authenticated cart: long TTL (for example 30 days) with sliding refresh on write.
- Anonymous cart: shorter TTL (for example 7 days) with sliding refresh on write.

Concurrency:

- Use optimistic concurrency with `version` field and compare-and-set semantics.
- Reject stale writes with conflict response.

### 4. Inventory consistency policy
Use two levels of inventory checks:

- Soft check on cart mutation: optional hint only (does not reserve stock).
- Hard check on checkout: mandatory atomic reservation.

Reservation lifecycle:

- `Reserve`: atomically reserve quantity if available.
- `Release`: return reserved stock on timeout/failure/cancel.
- `Commit`: consume reserved stock after successful order placement/payment authorization.

### 5. Oversell prevention invariant
Order placement must only succeed when reservation is atomically acquired for all required line items.

Required invariant:

- For each product, `available + reserved + sold` remains consistent by transactional rules.
- Concurrent checkout requests cannot drive committed quantity beyond on-hand stock.

### 6. Checkout orchestration and idempotency
Checkout flow (high level):

1. Validate cart and pricing snapshot.
2. Reserve inventory for all line items using idempotency key.
3. Initiate payment authorization.
4. On success: create order and commit reservation.
5. On failure: release reservation.

Idempotency requirements:

- Reserve/release/commit endpoints accept idempotency key.
- Duplicate requests with same key return prior result.
- Idempotency records expire with operationally safe TTL.

### 7. Security and abuse controls
Anonymous support must not weaken platform security.

Controls:

- Signed/secure/httpOnly cookie for `anonymousCartId`.
- Rate limiting by IP/device fingerprint for anonymous cart mutations.
- Max cart size and max per-item quantity limits.
- Input validation for all item and quantity updates.
- Structured audit logs for cart merge and checkout operations.

### 8. Observability requirements
Emit telemetry for:

- Cart create/update/remove/merge outcomes.
- Reservation acquire/release/commit outcomes.
- Reservation timeout/expiration cleanup.
- Conflict rate (optimistic concurrency failures).
- Oversell guardrail rejections (insufficient stock).

## Consequences

### Positive

- Supports guest shopping without registration friction.
- Preserves seamless continuity when guest users sign in.
- Provides deterministic inventory protection at checkout.
- Improves production resilience with idempotent operations.

### Trade-offs

- More complexity in merge and reservation flows.
- Requires stronger integration testing for concurrent scenarios.
- Adds lifecycle cleanup for reservations and idempotency records.

## Alternatives Considered

1. Authenticated carts only.
- Rejected: harms conversion and guest funnel.

2. Reserve stock on every add-to-cart.
- Rejected: poor UX, stock hoarding risk, and high operational overhead.

3. No reservation, validate stock only at order write.
- Rejected: high oversell risk under concurrency.

## Implementation Guardrails

- Keep cart endpoints thin and MediatR/CQRS-based.
- Keep implementation classes internal; expose only contracts/DTOs.
- Require cancellation tokens and structured logs in all paths.
- Use ProblemDetails and standardized error contracts.
- Add unit/integration/concurrency tests before rollout.

## Open Questions

1. Should anonymous cart TTL vary by region or traffic profile?
2. Should merge policy cap combined quantity by stock hint or only by per-item cart limit?
3. Should payment authorization happen before or after full reservation in all channels?
4. Do we need reservation partitioning strategy by warehouse in v1?
