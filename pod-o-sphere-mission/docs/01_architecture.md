# 01 Architecture

## Architectural approach

Use a hybrid SaaS architecture:

- MSSQL is the system of record for SaaS identity, tenants, billing, domains, jobs, and operational metadata.
- Supabase/Postgres stores content intelligence: episodes, transcripts, chunks, tags, topics, embeddings, search indexes, and RAG-ready records.
- n8n performs background orchestration and enrichment but remains hidden from end users.
- .NET APIs own security, tenancy, public API responses, and job creation.
- React/Next.js powers the brochure, admin surfaces, and public portal.

## Suggested apps

```txt
/apps
  /sales-site         # Brochure site
  /admin-portal       # Super Admin + Client Admin
  /public-portal      # Searchable show portal
/services
  /api                # ASP.NET Core API
  /worker             # .NET worker for jobs not assigned to n8n
/supabase
  /migrations
  /schemas
/mssql
  schema files
/n8n
  workflow exports and contracts
```

## Identity

Use Microsoft Entra ID as the main identity provider.

Recommended user categories:

- Pod-o-Sphere Super Admin
- Client Owner
- Client Admin
- Client Analyst/Editor
- Public visitor, anonymous by default

Public portal should not require login for search unless a client chooses private mode later.

## Tenant model

A tenant is a paying customer organization.
A show belongs to a tenant.
A portal belongs to a show.
A source belongs to a show.
Episodes/videos belong to a source and show.

## Domain model

Support two portal modes:

1. Pod-o-Sphere hosted slug:
   - `show.pod-o-sphere.com/{showSlug}`
   - or `{showSlug}.pod-o-sphere.com`

2. Client-owned custom domain:
   - `app.clientsdomain.com`
   - `archive.clientsdomain.com`
   - `show.clientsdomain.com`

MSSQL should store custom domain verification and routing metadata.

## Processing model

1. Client submits YouTube channel URL.
2. API creates an onboarding job in MSSQL.
3. n8n polling or webhook picks up the job.
4. n8n discovers videos and writes video metadata.
5. Transcript jobs are queued per video.
6. Transcript provider returns transcript text and timestamps.
7. Supabase stores raw transcript and chunked transcript records.
8. Enrichment jobs produce summary, topics, tags, entities, key questions, timeline segments, and embeddings.
9. Public portal queries Supabase through the .NET API.

## Why two databases?

MSSQL is a clean fit for existing .NET SaaS metadata, billing, tenancy, and operational workflows.
Supabase/Postgres is a strong fit for modern content intelligence workloads, including JSONB records, vector search through pgvector, full-text search, and RAG-oriented data structures.

## API boundary rule

The public portal and admin apps should call the .NET API. They should not directly expose unrestricted Supabase database access from the browser for tenant-sensitive workflows.

