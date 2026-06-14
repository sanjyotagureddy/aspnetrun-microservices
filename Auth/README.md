# Auth Postman Toolkit

This folder contains Postman assets to bootstrap local auth access without hardcoding secrets in source config.

Files:

- aspnetrun-auth.postman_collection.json
- aspnetrun-auth.postman_environment.json

Collection folders:

- Automation Flows
- Admin
- Tenant (Vendor)
- User

## What this covers

1. Create first user in Keycloak (admin API).
2. Set user password.
3. Resolve user subject/id.
4. Bootstrap first platform_admin membership in Auth API (development only).
5. Generate vendor token from token endpoint.
6. Generate user token from token endpoint.
7. Assign tenant role for vendor tenant.
8. Call profile/logout endpoints.

## Before running

1. Import collection and environment in Postman.
2. Fill required environment values:
   - keycloak_admin_password
   - dev_bootstrap_secret
   - user_token_client_secret (only if user_token_client_id is a confidential client)
   - vendor_token_client_secret (only if vendor_token_client_id is a confidential client)
3. Ensure Auth API is running and reachable by auth_api_base_url.
4. Ensure Keycloak realm commerce exists.
5. Token client bootstrap is included in the collection (Admin step 2a/2b, and setup automation flows), so `postman-dev-client` is created/validated automatically.

6. Ensure `auth_api_base_url` matches the running Auth API URL.
   - Typical local launch profile values:
   - `http://host.docker.internal:5000`
   - `http://localhost:65292`
   - `https://localhost:65291`

## Default local identities

Current environment defaults are prefilled for a local Sanjyot setup:

- `keycloak_admin_username`: `admin`
- `new_user_username`: `sanjyot`
- `new_user_email`: `sanjyot.auth.dev@local.test`
- `vendor_username`: `sanjyot-vendor`
- `vendor_email`: `sanjyot.vendor@local.test`
- `vendor_tenant_id`: `sanjyot-tenant`
- `new_user_password`: `ChangeMe123!`
- `vendor_password`: `ChangeMe123!`

If another developer is using this toolkit, update these values in `aspnetrun-auth.postman_environment.json` before running the flow.
Always change default passwords for shared environments.

## Postman Environment Variables

Set these values before execution.

Required:

- `keycloak_base_url`
- `commerce_realm`
- `auth_api_base_url`
- `token_scope_default` (default `openid profile email`)
- `token_scope_elevated` (default `openid profile email products.read products.write`)
- `keycloak_admin_username` (master realm admin user)
- `keycloak_admin_password`
- `new_user_username`
- `new_user_email`
- `new_user_first_name`
- `new_user_last_name`
- `new_user_password`
- `vendor_tenant_id`
- `dev_bootstrap_secret`
- `vendor_username`
- `vendor_email`
- `vendor_first_name`
- `vendor_last_name`
- `user_token_client_id`
- `vendor_password`
- `vendor_token_client_id`

Conditionally required:

- `user_token_client_secret` (required for confidential token client; can be empty for public client)
- User automation also accepts legacy `user_client_secret` and copies it into `user_token_client_secret` at runtime.
- `vendor_token_client_secret` (required for confidential token client; can be empty for public client)
- Vendor automation also accepts legacy `vendor_client_secret` and copies it into `vendor_token_client_secret` at runtime.
- If both primary and legacy secret variables are empty, pre-request scripts log a warning to Postman Console.
- If token endpoint returns `invalid_scope`, set `token_scope_default` to `openid profile email` and ensure custom scopes (for example `products.read`) are created in Keycloak before requesting them.
- `target_user_sub` (required only when running tenant role assignment for another user)
- `target_tenant_role` (used by tenant role assignment; default `buyer`)

Auto-populated by collection scripts:

- `admin_access_token`
- `keycloak_user_id`
- `keycloak_user_sub`
- `vendor_access_token`
- `vendor_refresh_token`
- `user_access_token`
- `user_refresh_token`

## Admin Token Lifetime

If admin calls expire too quickly, increase access token lifespan in Keycloak `master` realm.

- Keycloak Admin Console -> `master` realm -> Realm settings -> Tokens -> Access Token Lifespan

Use `3 minutes` for local bootstrap convenience if needed.

## Aspire Secret Parameters

Auth bootstrap settings are now expected from Aspire parameters (not hardcoded appsettings):

- `auth-dev-bootstrap-enabled`
- `auth-dev-bootstrap-shared-secret` (secret)

Set these in the Aspire run configuration or provide values when prompted by Aspire.
Use the same `auth-dev-bootstrap-shared-secret` value in Postman variable `dev_bootstrap_secret`.

## Automation flows (recommended)

Use Postman Runner and run one of these folders:

1. `Automation Flows/Flow 1 - Vendor Setup and Token (One-Time Setup)`
2. `Automation Flows/Flow 2 - Vendor Token Only`
3. `Automation Flows/Flow 3 - User Setup and Token (One-Time Setup)`
4. `Automation Flows/Flow 4 - User Token Only`

Notes:

- Setup flows are for first-time registration/bootstrap + token generation.
- Token-only flows are for repeat usage after setup already exists.
- Default token requests use least-privilege scope: `openid profile email products.read`.
- Use elevated token requests only when needed (`products.write`): `Tenant (Vendor)/7a. Generate Vendor Token (Elevated Scope)` and `User/8a. Generate User Token (Elevated Scope)`.
- `vendor_access_token` is produced by Flow 1 and Flow 2.
- `user_access_token` is produced by Flow 3 and Flow 4.

## Manual request order (advanced)

Run in this sequence:

1. Admin/1. Get Keycloak Admin Token
2. Admin/2. Ensure Commerce Realm Exists
3. Admin/2a. Ensure Vendor Token Client Exists
4. Admin/2b. Ensure User Token Client Exists
5. Admin/2c. Diagnose Vendor Token Client Settings
6. Admin/3. Register User (Keycloak Admin API)
7. Admin/4. Resolve User Id by Username
8. Admin/5. Set User Password
9. Tenant (Vendor)/6. Bootstrap First Platform Admin Membership (Development Only)
10. Tenant (Vendor)/7. Generate Vendor Token (Token Endpoint)
11. Tenant (Vendor)/7a. Generate Vendor Token (Elevated Scope) (Optional)
12. User/8. Generate User Token (Token Endpoint)
13. User/8a. Generate User Token (Elevated Scope) (Optional)
14. Tenant (Vendor)/9. Assign Tenant Role (Optional, Requires vendor platform_admin Token)
15. User/10. Get My Profile (Optional Validation)
16. User/11. Logout Endpoint (Optional Validation)

Usable tokens are available immediately after:

- step 7 or 8 in `vendor_access_token`
- step 9 or 10 in `user_access_token`
