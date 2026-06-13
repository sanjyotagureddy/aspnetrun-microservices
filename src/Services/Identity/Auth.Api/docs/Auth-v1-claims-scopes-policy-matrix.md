# Auth v1 Claims, Scopes, and Policy Matrix

Define the concrete v1 security contract for user auth, service-to-service auth, and tenant isolation.

## Status

Approved

## Date

2026-06-13

## 0. Decision Baseline (Approved for v1)

1. Identity provider: Keycloak (single realm for v1).
2. Token lifetimes: user access token 15 minutes, workload access token 5 minutes, user refresh token 14 days with rotation and reuse detection enabled.
3. Role and scope names: use the names in sections 5 and 6 with no aliases in v1.
4. Gateway enforcement: gateway validates JWT and enforces coarse route policies; downstream APIs still enforce full audience/scope/tenant policies (defense in depth).
5. Platform-admin cross-tenant operations: allowed only through explicit PlatformAdminPolicy with mandatory audit fields (`actor_sub`, `target_tenant_id`, `reason_code`, `change_ticket_id`, `approved_by`, `approved_at_utc`).

## 1. Authority and Token Settings

- Authority: Keycloak in v1.
- Issuer (`iss`): `https://auth.local/realms/commerce` (environment-specific).
- Discovery: `/.well-known/openid-configuration`.
- JWKS: `.../protocol/openid-connect/certs`.
- Access token format: JWT signed with asymmetric key (RS256 in v1).
- Clock skew tolerance: 60 seconds.

Environment URL decisions (v1):

- Development (in-cluster service-to-service):
  - Issuer: `http://keycloak:8080/realms/commerce`
  - Discovery: `http://keycloak:8080/realms/commerce/.well-known/openid-configuration`
  - JWKS: `http://keycloak:8080/realms/commerce/protocol/openid-connect/certs`
- Development (host/browser-facing):
  - Issuer: `http://localhost:8080/realms/commerce`
  - Discovery: `http://localhost:8080/realms/commerce/.well-known/openid-configuration`
  - JWKS: `http://localhost:8080/realms/commerce/protocol/openid-connect/certs`
- Test environment:
  - Issuer: `https://auth.test.aspnetrun.local/realms/commerce`
  - Discovery: `https://auth.test.aspnetrun.local/realms/commerce/.well-known/openid-configuration`
  - JWKS: `https://auth.test.aspnetrun.local/realms/commerce/protocol/openid-connect/certs`

Token lifetime guidance:

- User access token: 15 minutes.
- Workload access token: 5 minutes.
- Refresh token (user only): 14 days with rotation and reuse detection.

## 2. Audiences

Each API validates audience strictly.

- `aud=products-api`
- `aud=inventory-api`
- `aud=cart-api`
- `aud=order-api`
- `aud=gateway-yarp` (if gateway validates and relays tokens)

Rules:

- No API accepts wildcard audience.
- Tokens must include only required audiences.

## 3. Principal Types and Grant Flows

### User principal

- Grant: Authorization Code + PKCE.
- Typical subject: end user.
- Contains tenant and role claims.

### Workload principal

- Grant: Client Credentials.
- Typical subject: service client.
- Contains service scopes; does not represent end user identity.

## 4. Required Claims Contract

Required for all access tokens:

- `iss`: issuer.
- `aud`: one or more target audiences.
- `sub`: subject identifier.
- `exp`: expiration.
- `iat`: issued-at time.
- `jti`: unique token id.
- `client_id` or `azp`: authorized party.

Required for tenant-scoped user operations:

- `tenant_id`: tenant identifier (GUID in v1).
- `role`: one or more tenant roles.
- `scope`: delegated user permissions.

Recommended for delegated tracing and diagnostics:

- `sid`: session id.
- `acr`: authentication assurance context.

## 5. Tenant Roles (v1)

- `tenant_admin`: full tenant administration including catalog and tenant settings.
- `catalog_manager`: catalog write permissions for the tenant.
- `buyer`: shopping and checkout permissions for the tenant.
- `platform_admin`: explicit, audited cross-tenant operational role.

## 6. Scopes (v1)

Naming rule:

- Scope names are immutable for v1 once published.
- New permissions are additive; do not rename existing scopes in-place.

User scopes:

- `cart.read`
- `cart.write`
- `checkout.create`
- `orders.read`
- `products.read`
- `products.write` (tenant-bound and role-gated)

Workload scopes:

- `inventory.read`
- `inventory.reserve`
- `inventory.release`
- `inventory.commit`
- `order.create.internal`
- `payment.authorize.internal`
- `auth.introspect.internal`

Rules:

- Service scopes are never used by browser clients.
- User scopes do not imply cross-tenant access.
- Scope grants require matching audience.

## 7. Authorization Policies

### TenantMemberPolicy

Requirements:

- Authenticated principal.
- Valid `tenant_id` claim.
- Token `tenant_id` matches resource tenant context.

### CatalogWritePolicy

Requirements:

- Passes TenantMemberPolicy.
- Scope includes `products.write`.
- Role includes `tenant_admin` or `catalog_manager`.

### CheckoutPolicy

Requirements:

- Passes TenantMemberPolicy.
- Scope includes `checkout.create`.
- Role includes `buyer` or `tenant_admin`.

### WorkloadInventoryPolicy

Requirements:

- Workload principal only.
- Scope includes one of `inventory.reserve`, `inventory.release`, `inventory.commit`.
- Audience matches `inventory-api`.

### PlatformAdminPolicy

Requirements:

- Explicit `platform_admin` role.
- Operation is audited with actor, target tenant, and reason.

## 8. Endpoint Policy Matrix (v1)

Gateway note:

- Gateway performs token validation and coarse route authorization.
- Service APIs remain policy decision points for tenant/resource authorization.

### Cart API

- Get cart: TenantMemberPolicy + `cart.read`.
- Mutate cart: TenantMemberPolicy + `cart.write`.
- Merge anonymous cart: TenantMemberPolicy + `cart.write`.
- Checkout: CheckoutPolicy + idempotency key.

### Products API

- Read catalog: TenantMemberPolicy + `products.read`.
- Create product: CatalogWritePolicy.
- Update product: CatalogWritePolicy + product tenant ownership check.
- Delete product: CatalogWritePolicy + product tenant ownership check.

### Inventory API

- Public stock reads (internal): WorkloadInventoryPolicy or service read policy.
- Reserve/release/commit: WorkloadInventoryPolicy.
- Initialize stock (admin path): CatalogWritePolicy or platform-admin policy.

### Order API

- Create order from checkout: CheckoutPolicy or internal service policy for orchestration path.
- Read orders: TenantMemberPolicy + `orders.read` + ownership filter.

## 9. Tenant Boundary Invariants

- Tenant A cannot create, update, or delete Tenant B products.
- Tenant A cannot read Tenant B carts or orders.
- Repository queries must filter by `tenant_id` unless PlatformAdminPolicy explicitly allows otherwise.
- Header tenant values cannot override signed token tenant identity for external requests.

## 10. Token Validation Rules in Each API

- Validate issuer exactly.
- Validate audience exactly.
- Validate signature and signing key id.
- Validate expiration with bounded clock skew.
- Reject tokens without required claims for endpoint policy.
- Reject workload token at user-only endpoints and vice versa.

## 11. Audit and Security Logging Requirements

For authorization decisions and sensitive mutations log:

- Principal type (`user` or `workload`).
- `sub`, `client_id`/`azp`, `tenant_id`.
- Endpoint and operation.
- Decision (`allow` or `deny`) and reason.
- Correlation id and trace id.

Cross-tenant administrative operations must include:

- target tenant id.
- change reason code.
- operator identity.
- change ticket id.
- approver identity.
- approval timestamp (UTC).

## 12. Rollout Plan

1. Approve this contract.
2. Configure authority realms/clients/scopes/audiences.
3. Implement JwtBearer + policy handlers in Products API first.
4. Add tenant_id discriminator to product persistence and query filters.
5. Add tenant boundary integration tests.
6. Propagate policies to Cart, Inventory, and Order APIs.

## 13. Non-Goals (v1)

- Per-tenant signing keys.
- Multi-authority federation across regions.
- Fine-grained attribute-based policies beyond scope and role.
