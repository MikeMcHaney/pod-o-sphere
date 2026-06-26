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
- `GET http://localhost:5000/api/me`: authenticated identity summary; requires an Entra External ID access token.

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

## Checks

```bash
dotnet build PodOSphere.slnx
dotnet test PodOSphere.slnx
```

