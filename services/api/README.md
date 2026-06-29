# API

The API is the ASP.NET Core backend for tenant-aware admin workflows, health checks, identity resolution, and future onboarding/search endpoints. It runs on port `5000`.

## Run Locally

From the repository root:

```bash
dotnet run --project services/api
```

For hot reload during API work:

```bash
dotnet watch --project services/api
```

Useful endpoints:

- `GET http://localhost:5000/health`: process liveness.
- `GET http://localhost:5000/health/ready`: dependency readiness.
- `GET http://localhost:5000/api/status`: MSSQL and Supabase status summary.
- `GET http://localhost:5000/api/me`: resolves the authenticated Entra External ID token to a seeded Pod-o-Sphere `AppUser` and returns roles/memberships.
- `GET http://localhost:5000/api/admin/tenants`: SuperAdmin-only tenant list.
- `GET http://localhost:5000/api/admin/shows`: SuperAdmin-only show list for onboarding tools.
- `POST http://localhost:5000/api/admin/tenants/{tenantId}/invitations`: SuperAdmin-only invite creation. Returns the raw one-time token until email delivery is added.
- `POST http://localhost:5000/api/invitations/accept`: authenticated invite acceptance by token. The signed-in user's contact email must match the invitation email.
- `POST http://localhost:5000/api/admin/invitations/{invitationId}/revoke`: SuperAdmin-only pending invite revocation.
- `POST http://localhost:5000/api/show-claims`: authenticated show claim submission for later SuperAdmin review.
- `GET http://localhost:5000/api/admin/show-claims/pending`: SuperAdmin-only pending show-claim queue.
- `POST http://localhost:5000/api/admin/show-claims/{showClaimId}/approve`: SuperAdmin-only show-claim approval.
- `POST http://localhost:5000/api/admin/show-claims/{showClaimId}/reject`: SuperAdmin-only show-claim rejection.
- `POST http://localhost:5000/api/admin/shows/{showId}/youtube-source`: SuperAdmin-only YouTube source creation and inventory job enqueue.
- `POST http://localhost:5000/api/admin/youtube-demo`: SuperAdmin-only create show, create YouTube source, and enqueue demo inventory.
- `POST http://localhost:5000/internal/jobs/claim`: internal ingestion worker job claim.
- `POST http://localhost:5000/internal/jobs/{jobId}/heartbeat`: internal ingestion worker heartbeat.
- `POST http://localhost:5000/internal/jobs/{jobId}/complete`: internal ingestion worker completion with summary/metrics.
- `POST http://localhost:5000/internal/jobs/{jobId}/fail`: internal ingestion worker failure with retry intent.
- `POST http://localhost:5000/internal/sources/youtube/videos/upsert`: internal ingestion worker discovered-video upsert into Supabase content tables.

`SuperAdmin` is a platform role stored through `PlatformUserRoles`; tenant roles such as `TenantOwner` and `TenantAdmin` stay in `TenantUsers`.
`AppUsers.ContactEmail` is the application contact address; `PreferredUsername` is only an identity-provider hint.
SuperAdmin tenant-list access writes an `AuditEvents` record; invite and show-claim mutations use the same audit writer.
Invitation tokens are stored as hashes only; returned create-response tokens are for local/manual delivery until outbound email exists.
Show-claim review records approval or rejection only; tenant/show ownership transfer is intentionally deferred until the claim policy is designed.
Internal ingestion endpoints require the `X-PodOSphere-Internal-Token` header.
Use `inventoryMode: "Demo"` with `maxEpisodes: 5` for bounded YouTube demos.
Job and source statuses are centralized in API constants; job claims renew a 30-minute lease on heartbeat and stale leases are returned to `Pending` on the next claim attempt.

## Local Config

The API reads `appsettings.json`, environment variables, and .NET User Secrets.

For local development, initialize user secrets once:

```bash
dotnet user-secrets init --project services/api
```

Common settings:

```bash
dotnet user-secrets set --project services/api "PodOSphere:MssqlConnectionString" "Server=localhost,1433;Database=PodOSphere;User Id=sa;Password=Change_me_123!;TrustServerCertificate=True"
dotnet user-secrets set --project services/api "PodOSphere:SupabaseUrl" "http://localhost:54321"
dotnet user-secrets set --project services/api "PodOSphere:SupabasePostgresConnectionString" "Host=localhost;Port=54322;Database=postgres;Username=postgres;Password=postgres"
dotnet user-secrets set --project services/api "PodOSphere:SupabaseServiceRoleKey" "<local-or-project-service-role-key>"
dotnet user-secrets set --project services/api "PodOSphere:InternalIngestionToken" "<local-long-random-token>"
```

Entra External ID settings:

```bash
dotnet user-secrets set --project services/api "EntraExternalId:Authority" "https://<tenant-subdomain>.ciamlogin.com/"
dotnet user-secrets set --project services/api "EntraExternalId:TenantId" "<directory-tenant-id>"
dotnet user-secrets set --project services/api "EntraExternalId:ClientId" "<api-app-client-id>"
dotnet user-secrets set --project services/api "EntraExternalId:Audience" "api://<api-app-client-id>"
```

Do not commit secrets. Use `.env.example` and `appsettings.json` only as shape/reference files.

## Local Dependencies

Start MSSQL:

```bash
docker compose up -d mssql
```

Start Supabase locally when you need the content database:

```bash
supabase start
```

Apply database scripts/migrations as described in the root README.

For the current ingestion process map, use [`INGESTION_WORKFLOW.md`](./INGESTION_WORKFLOW.md).
For an end-to-end ingestion loop without n8n, use [`INGESTION_SMOKE.md`](./INGESTION_SMOKE.md).

## Checks

```bash
dotnet build PodOSphere.slnx
dotnet test PodOSphere.slnx
```
