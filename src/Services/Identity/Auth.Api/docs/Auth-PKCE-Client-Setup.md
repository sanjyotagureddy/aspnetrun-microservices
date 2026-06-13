# Auth PKCE Client Setup (v1)

Configure Authorization Code + PKCE clients in Keycloak and keep Auth.Api configuration aligned.

## Required Keycloak Client Settings

- Client type: public (or confidential if you explicitly use client secret in backend exchange).
- Standard flow enabled: true.
- Direct access grants: false for browser clients.
- Valid redirect URIs must exactly match configured Auth PKCE client redirect URIs.
- Web origins should be restricted to the frontend origin(s).

## Auth.Api Configuration Contract

Use `Auth:PkceClients` to define allowed clients and redirect URIs.

Example:

```json
{
  "Auth": {
    "WebClientId": "web-app",
    "WebClientScope": "openid profile email",
    "PkceClients": [
      {
        "ClientId": "web-app",
        "Scope": "openid profile email",
        "RedirectUris": [
          "http://localhost:5173/auth/callback",
          "http://localhost:3000/auth/callback"
        ]
      }
    ]
  }
}
```

## Runtime Enforcement

- `/api/v1/auth/login/start` accepts only configured PKCE client IDs.
- Redirect URI must be in the selected client allow-list.
- `code_challenge_method` must be `S256`.

## AppHost Development Wiring

AppHost sets `Auth__PkceClients__...` environment variables for local dev defaults.

Override values using user-secrets or environment variables for team-specific frontend ports.

## Postman Bootstrap Toolkit

For local bootstrap, user creation, tenant membership setup, token generation, and logout/profile calls, use:

- [../../../../../Auth/aspnetrun-auth.postman_collection.json](../../../../../Auth/aspnetrun-auth.postman_collection.json)
- [../../../../../Auth/aspnetrun-auth.postman_environment.json](../../../../../Auth/aspnetrun-auth.postman_environment.json)
- [../../../../../Auth/README.md](../../../../../Auth/README.md)

This is the recommended local bootstrap path instead of hardcoding secrets in development config files.

## Development Bootstrap For First Tenant Membership

When no user has `platform_admin` yet, use the development-only bootstrap endpoint to create the initial membership record in `auth_user_tenant_memberships`.

Conditions:

- Environment must be Development.
- `DevBootstrap:Enabled` must be `true`.
- Request must include `DevBootstrap:SharedSecret` exactly.

Recommended configuration source:

- Provide `DevBootstrap` values from Aspire parameters:
  - `auth-dev-bootstrap-enabled`
  - `auth-dev-bootstrap-shared-secret` (secret)

Endpoint:

- `POST /api/v1/auth/internal/bootstrap/memberships`

Request example:

```json
{
  "subject": "<keycloak-user-sub>",
  "tenantId": "tenant-alpha",
  "role": "platform_admin",
  "secret": "local-bootstrap-secret-change-me"
}
```

After bootstrap:

1. Sign in as this user and obtain a normal user access token.
2. Use `/api/v1/auth/internal/tenants/{tenantId}/memberships` for ongoing role assignments.
3. Disable bootstrap (`DevBootstrap:Enabled=false`) or rotate/remove the shared secret.
