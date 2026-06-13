# Auth Postman Toolkit

This folder contains Postman assets to bootstrap local auth access without hardcoding secrets in source config.

Files:

- aspnetrun-auth.postman_collection.json
- aspnetrun-auth.postman_environment.json

Collection folders:

- Admin
- User
- Tenant (Vendor)

## What this covers

1. Create first user in Keycloak (admin API).
2. Set user password.
3. Resolve user subject/id.
4. Bootstrap first platform_admin membership in Auth API (development only).
5. Generate vendor token from token endpoint.
6. Generate user token from token endpoint.
6. Call profile/logout endpoints.
7. Assign tenant role for vendor tenant.

## Before running

1. Import collection and environment in Postman.
2. Fill required environment values:
   - keycloak_admin_password
   - dev_bootstrap_secret
   - user_token_client_secret (if your token client is confidential)
3. Ensure Auth API is running and reachable by auth_api_base_url.
4. Ensure Keycloak realm commerce exists.
5. Ensure postman-dev-client exists in Keycloak realm with direct access grants enabled for local development token generation.

6. Ensure `auth_api_base_url` matches the running Auth API URL.
    - Typical local launch profile values:
   - `http://host.docker.internal:5000`
       - `http://localhost:65292`
       - `https://localhost:65291`

## Default local identities

Current environment defaults are prefilled for a local Sanjyot setup:

- `keycloak_admin_username`: `sanjyot-admin`
- `new_user_username`: `sanjyot`
- `new_user_email`: `sanjyot.admin@local.test`
- `vendor_username`: `sanjyot`
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
- `keycloak_admin_username` (master realm admin user)
- `keycloak_admin_password`
- `new_user_username`
- `new_user_email`
- `new_user_first_name`
- `new_user_last_name`
- `new_user_password`
- `vendor_tenant_id`
- `dev_bootstrap_secret`
- `user_token_client_id`
- `vendor_username`
- `vendor_password`
- `vendor_token_client_id`

Conditionally required:

- `user_token_client_secret` (required for confidential token client; can be empty for public client)
- `vendor_token_client_secret` (required for confidential token client; can be empty for public client)
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

## Request order

Run in this sequence:

1. Admin/1. Get Keycloak Admin Token
2. Admin/2. Ensure Commerce Realm Exists
3. Admin/3. Register User (Keycloak Admin API)
4. Admin/4. Resolve User Id by Username
5. Admin/5. Set User Password
6. Tenant (Vendor)/5. Bootstrap First Platform Admin Membership (Development Only)
7. Tenant (Vendor)/6. Generate Vendor Token (Token Endpoint)
8. Tenant (Vendor)/7. Assign Tenant Role (Requires vendor platform_admin Token)
9. User/7. Generate User Token (Token Endpoint)
10. User/8. Get My Profile
11. User/9. Logout Endpoint

After step 6, you can call protected APIs using user_access_token.
