# Admin Portal

The admin portal is the authenticated operator/client surface for Pod-o-Sphere. It runs as a Next.js app on port `3001` and talks to the ASP.NET Core API.

## Run Locally

From the repository root:

```bash
npm install
npm run dev --workspace @pod-o-sphere/admin-portal
```

Open `http://localhost:3001`.

For a production-style local build:

```bash
npm run build --workspace @pod-o-sphere/admin-portal
npm run start --workspace @pod-o-sphere/admin-portal
```

## Local Config

Copy the app-local example file:

```bash
cp apps/admin-portal/.env.example apps/admin-portal/.env.local
```

Required values:

- `NEXT_PUBLIC_API_URL`: API base URL, usually `http://localhost:5000`.
- `NEXT_PUBLIC_ENTRA_CLIENT_ID`: Admin portal SPA app registration client ID.
- `NEXT_PUBLIC_ENTRA_TENANT_SUBDOMAIN`: External ID tenant subdomain, the part before `.ciamlogin.com`.
- `NEXT_PUBLIC_ENTRA_TENANT_ID`: External ID directory tenant ID. MSAL uses it to trust the GUID issuer host returned by CIAM discovery.
- `NEXT_PUBLIC_ENTRA_API_SCOPE`: API scope exposed by the API app registration, usually `api://<api-client-id>/access_as_user`.

The API must be running for the "call API" flow to work:

```bash
dotnet run --project services/api
```

## Checks

```bash
npm run typecheck --workspace @pod-o-sphere/admin-portal
npm run build --workspace @pod-o-sphere/admin-portal
```
