# ADR-0002: Authentication and Tenant Isolation Strategy

## Status

Accepted

## Date

2026-06-13

## Tracking

- Implementation checklist: [Cart-Implementation-Checklist.md](../../Cart/Cart.Api/docs/Cart-Implementation-Checklist.md)
- Functional scenario catalog: [Website-Functional-Scenario-Catalog.md](../../Cart/Cart.Api/docs/Website-Functional-Scenario-Catalog.md)
- Auth contract matrix: [Auth-v1-claims-scopes-policy-matrix.md](Auth-v1-claims-scopes-policy-matrix.md)
- Auth implementation checklist: [Auth-Service-Implementation-Checklist.md](Auth-Service-Implementation-Checklist.md)

## Decision Record

- v1 baseline decisions (provider, token lifetimes, gateway strategy, and platform-admin audit fields) are defined in `Auth-v1-claims-scopes-policy-matrix.md` section "0. Decision Baseline (Approved for v1)".

## Context

The platform must support:

- End-user authentication for website user journeys.
- Service-to-service authentication for internal API calls.
- Strict tenant isolation so one tenant cannot mutate another tenant's catalog or orders.

Current risk:

- Product write endpoints are not currently protected by tenant-scoped authorization policies.
- Product persistence model is not tenant-discriminated yet.

## Decision

Adopt a single identity authority with separate user/workload auth flows and enforce tenant-scoped authorization across business services.

### 1. Principal types and flows

- User principal: Authorization Code + PKCE.
- Workload principal: Client Credentials.

Both token types are signed by the same authority but must have separate scope sets and authorization policies.

### 2. Tenant identity contract

Required claims:

- `sub`: user or client subject.
- `tenant_id`: owning tenant identifier.
- `scope`: API permissions.
- `role`: tenant role where applicable.
- `azp` or `client_id`: authorized party.

Rules:

- Tenant-scoped endpoints require `tenant_id` claim.
- Missing or malformed `tenant_id` yields unauthorized/forbidden.
- Header-provided tenant values cannot override signed token tenant identity for external calls.

### 3. Authorization model

Default posture: deny by default.

Policies:

- `TenantMemberPolicy`: tenant match required for read within tenant boundary.
- `CatalogWritePolicy`: tenant match + role in (`tenant_admin`, `catalog_manager`).
- `PlatformAdminPolicy`: explicit elevated role for controlled cross-tenant operations.

### 4. Data isolation model

Use shared database with mandatory `tenant_id` discriminator in business tables for v1.

Guardrails:

- Every tenant-owned entity includes `tenant_id`.
- Repository queries must always filter by `tenant_id` unless `PlatformAdminPolicy` explicitly applies.
- Unique indexes include `tenant_id` for tenant-owned uniqueness constraints.

### 5. Service-to-service tenant propagation

For delegated operations:

- Internal service calls include tenant context from validated upstream identity.
- Downstream service enforces tenant match against propagated context and policy.
- Integration events carry `tenant_id` metadata.

For platform operations:

- Use explicit platform scope and audited execution path.

### 6. Product service hard requirement

Product create/update/delete operations must enforce tenant ownership.

Invariant:

- Tenant A cannot create/update/delete Tenant B products.

### 7. Observability and audit

Log and audit:

- Principal type (user/workload), subject, tenant_id, endpoint, decision.
- Authorization denials and tenant boundary violations.
- Platform-admin cross-tenant actions with reason codes.

## Consequences

### Positive

- Prevents cross-tenant data mutation and leakage.
- Aligns user and service authentication under one authority model.
- Enables auditable, policy-driven exceptions for platform operations.

### Trade-offs

- Adds policy and claim-management complexity.
- Requires schema and query refactors to carry tenant_id.
- Requires stronger test coverage across authorization and tenant boundaries.

## Alternatives Considered

1. Header-only tenant identity without token claim binding.

- Rejected: prone to spoofing and boundary bypass.

1. Tenant checks only at API edge.

- Rejected: unsafe if downstream services/repositories do not enforce tenant constraints.

1. Separate auth authorities per tenant from day one.

- Rejected for v1 due to high operational complexity.

## Implementation Guardrails

- Tenant checks must exist at API policy and repository query layers.
- No product mutation endpoint may ship without tenant policy enforcement.
- Platform-admin cross-tenant actions require explicit policy and audit log.
- Add unit, integration, and authorization tests for tenant boundaries.

## Open Questions

1. Should tenant_id be GUID-only or support slug aliases in public APIs?
2. Do we need per-tenant signing keys in future compliance regions?
3. Should we adopt PostgreSQL RLS in a later phase for defense in depth?
