# Pod-o-Sphere

Pod-o-Sphere is a multi-tenant platform that turns podcast and YouTube archives into branded, searchable topic portals. This repository currently contains the minimal Phase 0 application skeleton described in the [mission packet](./pod-o-sphere-mission/README.md).

## Repository layout

```text
apps/
  sales-site/       Brochure and conversion site (port 3000)
  admin-portal/     Super Admin and Client Admin shell (port 3001)
  public-portal/    Brandable audience portal shell (port 3002)
services/
  api/              ASP.NET Core API (port 5000)
  worker/           Reserved .NET background worker
mssql/              Authoritative SaaS metadata schema reference
supabase/           Content intelligence schema and migrations
n8n/                Private workflow contracts; never a product surface
```

MSSQL owns identity mappings, tenancy, billing, shows, domains, sources, and processing jobs. Supabase/Postgres owns episodes, transcripts, enrichment, search, embeddings, and future RAG data. Browser applications access tenant-sensitive data through the .NET API.

## Prerequisites

- Node.js 22 or newer and npm
- .NET SDK 10
- Docker Desktop for local MSSQL
- Supabase CLI for the local content database

## Start the applications

Install web dependencies and create local configuration:

```bash
npm install
cp .env.example .env
```

Run each web surface in its own terminal:

```bash
npm run dev --workspace @pod-o-sphere/sales-site
npm run dev --workspace @pod-o-sphere/admin-portal
npm run dev --workspace @pod-o-sphere/public-portal
```

Run the API and optional worker:

```bash
dotnet run --project services/api
dotnet run --project services/worker
```

The API exposes liveness at `http://localhost:5000/health` and dependency readiness at `http://localhost:5000/health/ready`. `GET /api/status` reports live MSSQL and Supabase/Postgres status without returning connection details. `GET /api/me` requires a Microsoft Entra External ID access token issued for the API registration.

Copy `apps/admin-portal/.env.example` to `apps/admin-portal/.env.local` and fill in the admin portal application ID, tenant subdomain, directory tenant ID, and exposed API scope. API identity settings are stored locally with .NET User Secrets under the `EntraExternalId` section; no client secret is required for the browser-to-API delegated flow.

## Databases

Start MSSQL, then apply the numbered scripts under [`mssql`](./mssql) in order with your preferred SQL client. `001_core_metadata_schema.sql` is the mission-packet baseline; later scripts are forward migrations.

```bash
docker compose up -d mssql
```

For Supabase, initialize/link the CLI as appropriate for the target project and apply the checked-in migrations:

```bash
supabase start
supabase db reset
```

The `001` MSSQL schema and initial Supabase migration are mission-packet baselines copied unchanged. Later numbered scripts are Pod-o-Sphere forward migrations; application mappings must reflect the full ordered result.

## Checks

```bash
npm run typecheck
npm run build
dotnet build PodOSphere.slnx
dotnet test PodOSphere.slnx
```

Phase 0 deliberately excludes real authentication, database access, onboarding endpoints, and search behavior. Those features begin in the later phases documented under [`pod-o-sphere-mission/docs`](./pod-o-sphere-mission/docs).
