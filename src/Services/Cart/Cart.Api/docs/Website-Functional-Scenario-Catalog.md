# Website Functional Scenario Catalog

Capture end-to-end scenarios required for a production-ready eCommerce website experience.

## Status legend

- [ ] Not verified
- [x] Verified

## A. Discovery and Product Browsing

- [ ] User can browse product listing with pagination and filtering.
- [ ] User can search products by keyword and category.
- [ ] Product detail page shows price, stock hint, and image.
- [ ] Product detail page handles missing product with friendly not-found behavior.
- [ ] Product detail page handles temporary inventory lookup failure with graceful fallback.

## B. Anonymous Session and Cart

- [ ] Anonymous user receives secure anonymous cart identity cookie.
- [ ] Anonymous user can add first item to cart.
- [ ] Anonymous user can add multiple different items.
- [ ] Anonymous user can update quantity within allowed limits.
- [ ] Anonymous user can remove item.
- [ ] Anonymous user can clear entire cart.
- [ ] Anonymous cart persists across browser refresh and revisit.
- [ ] Anonymous cart expires according to TTL policy.

## C. Authenticated User Cart

- [ ] Auth service issues valid access token with required cart claims.
- [ ] Expired token is rejected and refresh flow succeeds where enabled.
- [ ] Invalid token signature/issuer/audience is rejected.
- [ ] Signed-in user can retrieve existing cart from prior session.
- [ ] Signed-in user can add/update/remove items.
- [ ] Signed-in cart persists across devices/sessions.
- [ ] Cart ownership is enforced; user cannot access another user's cart.

## D. Merge Scenarios (Anonymous to Authenticated)

- [ ] Sign-in event with valid identity triggers merge flow exactly once.
- [ ] Sign-in with anonymous cart only creates authenticated cart from anonymous content.
- [ ] Sign-in with authenticated cart only leaves authenticated cart unchanged.
- [ ] Sign-in with both carts merges according to approved merge policy.
- [ ] Duplicate line items merge correctly and respect quantity caps.
- [ ] Source anonymous cart is deleted after successful merge.
- [ ] Merge operation is idempotent when sign-in callback is retried.

## E. Cart Validation and UX Rules

- [ ] Quantity lower bound is enforced.
- [ ] Quantity upper bound per line item is enforced.
- [ ] Maximum unique line items per cart is enforced.
- [ ] Invalid product IDs are rejected with clear validation errors.
- [ ] Out-of-stock hints are shown without silently mutating cart quantities.

## F. Checkout Readiness

- [ ] Empty cart cannot enter checkout.
- [ ] Cart totals are recalculated server-side before checkout.
- [ ] Price snapshot mismatch is detected and surfaced.
- [ ] Checkout requires required customer data and shipping details.

## G. Inventory Reservation and Oversell Protection

- [ ] Reservation succeeds when stock is sufficient.
- [ ] Reservation fails atomically when any line item is insufficient.
- [ ] Concurrent checkout (50 + 60 against stock 100) allows only valid reservation.
- [ ] Reservation timeout returns stock to available pool.
- [ ] Release endpoint restores stock correctly for canceled/failed checkouts.
- [ ] Commit endpoint finalizes stock deduction exactly once.
- [ ] Duplicate reserve/release/commit requests are idempotent.

## H. Payment and Order Coordination

- [ ] Payment success path commits reservation and creates order.
- [ ] Payment authorization failure releases reservation.
- [ ] Payment timeout releases reservation after safe policy window.
- [ ] Duplicate payment callback does not duplicate orders.
- [ ] Order creation failure after reservation triggers compensation.

## I. Post-Checkout Experience

- [ ] Successful checkout clears or locks purchased cart state.
- [ ] Order confirmation page displays final order details.
- [ ] Order history includes newly placed order.
- [ ] User can re-order from previous order where policy allows.

## J. Failure and Resilience Scenarios

- [ ] Redis temporary outage returns safe degraded response without corrupting cart state.
- [ ] Inventory service timeout during checkout returns retry-safe response.
- [ ] Network retry does not duplicate cart mutation due to idempotency/etag checks.
- [ ] Partial downstream failure preserves consistency through compensation.
- [ ] Circuit-breaker or resilience policy engages under repeated downstream faults.

## K. Security and Abuse Protection

- [ ] Anonymous cart cookie is secure, signed, and httpOnly.
- [ ] Anonymous cart mutation endpoints are rate-limited.
- [ ] Authenticated endpoints enforce authorization consistently.
- [ ] Tenant claim is required and validated for tenant-scoped endpoints.
- [ ] Tenant A cannot create/update/delete Tenant B catalog products.
- [ ] Product write endpoints require tenant role (`tenant_admin` or `catalog_manager`).
- [ ] Platform-admin cross-tenant operations are explicit, audited, and policy-gated.
- [ ] Input payloads are validated against injection and malformed values.
- [ ] Sensitive data is excluded or redacted from logs.

## L. Observability and Operations

- [ ] Cart operation logs include correlation identifiers.
- [ ] Reservation lifecycle metrics are emitted.
- [ ] Checkout traces span cart, inventory, payment, and order services.
- [ ] Alerts exist for reservation leak, high conflict rate, and checkout failure spikes.
- [ ] Operational runbook exists for stuck reservations and replay paths.

## M. Accessibility, Mobile, and Performance

- [ ] Cart and checkout flows are usable on mobile breakpoints.
- [ ] Accessibility checks pass for keyboard and screen-reader navigation.
- [ ] Core cart and checkout pages meet performance budget targets.
- [ ] Error states are understandable and actionable on small screens.

## N. Test Mapping and Evidence

- [ ] Each scenario maps to at least one automated unit, integration, or E2E test.
- [ ] Concurrency-sensitive scenarios include load/concurrency test coverage.
- [ ] CI pipeline reports pass/fail status for mapped scenario suites.
- [ ] Coverage report confirms no regression in changed critical areas.

## O. Multi-Tenant Product Ownership Scenarios

- [ ] Tenant-specific product creation stamps tenant ownership metadata.
- [ ] Tenant-scoped product queries return only that tenant's catalog by default.
- [ ] Update product fails with forbidden when tenant does not own product.
- [ ] Delete product fails with forbidden when tenant does not own product.
- [ ] Bulk product import/export operations enforce tenant boundary and role checks.
- [ ] Product events include tenant identity and preserve tenant boundary in downstream consumers.

## Release Gate

Before production rollout, all critical scenarios in sections B through J must be verified and signed off by Cart, Inventory, and Order owners.
