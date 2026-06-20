# Pod-o-Sphere Mission Packet

Codename: **Pod-o-Sphere**

Goal: Build a multi-tenant podcast / YouTube intelligence platform that turns a creator's backlog into a branded, searchable, topic-rich public portal.

Primary product promise:

> Turn your podcast archive into a searchable topic universe.

This packet gives Codex a phased build plan, architecture map, database split, schema starters, workflow contracts, and implementation prompts.

## Repository shape

```txt
/docs
  00_product_brief.md
  01_architecture.md
  02_phase_plan.md
  03_data_model.md
  04_onboarding_workflow.md
  05_public_portal.md
  06_identity_and_tenancy.md
  07_codex_prompts.md
/mssql
  001_core_metadata_schema.sql
/supabase
  /migrations
    202606190001_initial_content_intelligence.sql
  /schemas
    content_intelligence.sql
/n8n/contracts
  youtube_onboarding_contract.md
/codex
  kickoff_prompt.md
```

## Technical north star

- Entra ID for identity.
- Brochure sales site for marketing and conversion.
- Super Admin backend for Pod-o-Sphere operators.
- Client Admin backend for show owners to control branding and settings.
- Public branded portal per client/show, such as `show.pod-o-sphere.com/{slug}` or `app.clientsdomain.com`.
- MSSQL for accounts, billing, tenancy, domains, jobs, and SaaS metadata.
- Supabase/Postgres for episode intelligence, transcripts, semantic search, topic/tag maps, chatbot/RAG data, and modern indexing.
- n8n behind the curtain for ingestion and enrichment workflows.

