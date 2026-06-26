# Worker

The worker is reserved for future background processing. For now it is a minimal .NET worker-service shell.

## Run Locally

From the repository root:

```bash
dotnet run --project services/worker
```

For hot reload during worker work:

```bash
dotnet watch --project services/worker
```

## Local Config

The current worker shell does not require app-specific settings beyond `services/worker/appsettings.json`.

Future job-processing work should keep secrets in .NET User Secrets or environment variables, not in committed config files.

## Checks

```bash
dotnet build PodOSphere.slnx
dotnet test PodOSphere.slnx
```

