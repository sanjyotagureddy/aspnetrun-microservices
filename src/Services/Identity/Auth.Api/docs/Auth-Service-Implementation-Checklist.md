# Auth Service Implementation Checklist

Track delivery of user authentication, service-to-service authentication, tenant isolation, and policy enforcement.

## Status legend

- [ ] Not started
- [x] Completed

## Inputs and References

- [Auth-v1-claims-scopes-policy-matrix.md](Auth-v1-claims-scopes-policy-matrix.md)
- [ADR-0002-auth-and-tenant-isolation.md](ADR-0002-auth-and-tenant-isolation.md)
- [Auth-PKCE-Client-Setup.md](Auth-PKCE-Client-Setup.md)
- [Website-Functional-Scenario-Catalog.md](../../Cart/Cart.Api/docs/Website-Functional-Scenario-Catalog.md)

## Phase 0: Architecture and Contracts

- [x] Confirm Keycloak as v1 authority and deployment topology.
- [x] Approve issuer, discovery, and JWKS URLs per environment.
- [x] Freeze claims contract (`sub`, `tenant_id`, `role`, `scope`, `aud`, `jti`, `azp/client_id`).
- [x] Freeze scope catalog and role catalog from auth matrix.
- [x] Freeze audience map for cart-api, products-api, inventory-api, order-api, gateway-yarp.
- [x] Publish token lifetime policy (15m user access, 5m workload access, 14d refresh with rotation).

## Phase 1: Infrastructure and Bootstrap

- [x] Add Keycloak container/resource in AppHost.
- [x] Add PostgreSQL database/schema for auth domain data if needed.
- [x] Add Redis for revocation/replay/idempotency cache.
- [x] Configure secrets and environment wiring for dev/test.
- [x] Add readiness/liveness probes for authority and auth-api.

## Phase 2: Auth Service Skeleton

- [x] Create Auth service project with Minimal API and ServiceDefaults.
- [x] Add OpenAPI and standardized ProblemDetails handling.
- [x] Add structured logging and correlation propagation.
- [x] Add MediatR + validation pipeline for auth endpoints.
- [x] Add internal health endpoints and diagnostics surface.

## Phase 3: User Authentication Capabilities

- [x] Configure Authorization Code + PKCE client(s) for web app.
- [x] Implement login redirect/start endpoint contracts.
- [x] Implement callback/session exchange flow contracts.
- [x] Implement refresh token flow with rotation and reuse detection policy.
- [x] Implement logout/revoke session flow.
- [x] Define user profile and tenant membership retrieval endpoint.

## Phase 4: Service-to-Service Authentication Capabilities

- [x] Register service clients and workload scopes.
- [x] Enforce client credentials flow for internal callers.
- [x] Add service credential lifecycle policy (rotation, expiry, revocation).
- [x] Add internal introspection/validation endpoint policy where required.
- [x] Deny workload token usage on user-only endpoints.

## Phase 5: Tenant and RBAC Enforcement

- [x] Implement tenant membership model and role assignments.
- [x] Implement role mappings for tenant_admin, catalog_manager, buyer, platform_admin.
- [x] Ensure tenant_id is mandatory for tenant-scoped tokens.
- [x] Implement policy helpers for TenantMemberPolicy, CatalogWritePolicy, CheckoutPolicy, PlatformAdminPolicy.
- [x] Add explicit cross-tenant exception path only for platform_admin policy.

## Phase 6: Gateway and API Policy Integration

- [x] Configure gateway JWT validation with issuer/audience checks.
- [x] Configure coarse route policies at gateway.
- [ ] Configure downstream APIs with full JwtBearer validation and policy checks.
- [x] Enforce audience/scope/tenant checks in Products API write endpoints first.
- [ ] Enforce user vs workload principal separation in APIs.

## Phase 7: Product Tenant Boundary Hardening

- [ ] Add tenant_id discriminator to product persistence model.
- [ ] Add tenant-scoped repository filters for reads/writes.
- [ ] Add tenant ownership checks for update and delete paths.
- [ ] Add unique index strategy including tenant_id where required.
- [ ] Block release if tenant A can mutate tenant B catalog.

## Phase 8: Security, Audit, and Compliance

- [ ] Audit log all auth decisions with actor, tenant, endpoint, decision.
- [ ] Enforce mandatory audit fields for platform-admin cross-tenant operations.
- [ ] Add brute-force and abuse protections on auth endpoints.
- [ ] Add signing key rotation runbook and validation checks.
- [ ] Add secret/credential rotation runbook for service clients.

## Phase 9: Reliability and Resilience

- [ ] Add retry/timeouts/circuit policies for auth dependencies.
- [ ] Define degraded-mode behavior for temporary authority outage.
- [ ] Add token validation cache policy with safe TTLs.
- [ ] Add observability dashboards for login failures and token errors.
- [ ] Add alerting for unusual deny spikes and cross-tenant deny events.

## Phase 10: Testing and Quality Gates

- [ ] Unit tests for claim parsing, role mapping, and policy handlers.
- [ ] Integration tests for user auth flow and refresh rotation rules.
- [ ] Integration tests for client credentials and workload scope enforcement.
- [ ] Integration tests for tenant boundary and cross-tenant denial scenarios.
- [ ] End-to-end tests for cart/product/order flows under auth policies.
- [ ] Coverage report for changed auth and policy enforcement code.

## Phase 11: Rollout and Migration

- [ ] Roll out to non-production with synthetic tenants and workloads.
- [ ] Validate gateway + downstream policy behavior with canary traffic.
- [ ] Enable Products API enforcement first; then Cart/Inventory/Order.
- [ ] Run incident rehearsal for key rotation and auth outage scenarios.
- [ ] Complete production readiness review and sign-off.

## Definition of Done

- [ ] User auth and workload auth both functional and policy-enforced.
- [ ] Tenant isolation guarantees verified in automated tests.
- [ ] Product cross-tenant mutation is impossible by policy and data guards.
- [ ] Gateway and downstream APIs are aligned on auth contract.
- [ ] Operational runbooks, alerts, and audit requirements are in place.
