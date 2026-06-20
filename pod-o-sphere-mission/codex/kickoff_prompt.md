# Codex Kickoff Prompt

You are helping build **Pod-o-Sphere**, a multi-tenant micro SaaS that turns podcast and YouTube archives into branded searchable topic portals.

Read these files first:

- `README.md`
- `docs/00_product_brief.md`
- `docs/01_architecture.md`
- `docs/02_phase_plan.md`
- `docs/03_data_model.md`
- `docs/04_onboarding_workflow.md`
- `docs/05_public_portal.md`
- `docs/06_identity_and_tenancy.md`
- `mssql/001_core_metadata_schema.sql`
- `supabase/migrations/202606190001_initial_content_intelligence.sql`
- `n8n/contracts/youtube_onboarding_contract.md`

Initial implementation target:

1. Create a runnable monorepo skeleton.
2. Add ASP.NET Core API with health checks and placeholder auth middleware.
3. Add React/Next.js admin portal shell.
4. Add React/Next.js public portal shell.
5. Add brochure sales site shell.
6. Add MSSQL and Supabase schema scripts exactly as source-controlled references.
7. Add README instructions for local development.

Do not overbuild. Keep Phase 0 minimal, but structure it so Phase 1 can add Entra ID, tenant roles, branding settings, and onboarding jobs cleanly.

Important constraints:

- n8n is behind the curtain and must not be exposed as the sold product.
- MSSQL owns accounts, billing, tenancy, domains, and jobs.
- Supabase/Postgres owns content intelligence, transcripts, search, tags, topics, embeddings, and RAG-ready data.
- Public portal must be brandable per show.
- Client admin must eventually support hex color pickers, logo upload, and banner upload.

