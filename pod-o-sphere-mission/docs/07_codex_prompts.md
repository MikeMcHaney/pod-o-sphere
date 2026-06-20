# 07 Codex Prompts

## Prompt 1: Repo scaffold

Create a monorepo for Pod-o-Sphere with apps for a brochure sales site, admin portal, public portal, an ASP.NET Core API, and a .NET worker. Add folders for MSSQL schema, Supabase migrations, and n8n workflow contracts. Keep implementation minimal but runnable.

## Prompt 2: MSSQL metadata schema

Using `/mssql/001_core_metadata_schema.sql`, create EF Core entities and DbContext mappings for tenants, users, roles, shows, portal settings, domains, sources, and processing jobs. Add migrations if this repo uses EF migrations, but keep raw SQL as the source reference.

## Prompt 3: Supabase schema

Using `/supabase/migrations/202606190001_initial_content_intelligence.sql`, create the initial content intelligence schema for episodes, transcripts, chunks, tags, topics, search documents, and embeddings. Include pgvector extension support.

## Prompt 4: Admin branding page

Build a Client Admin branding settings page with logo upload placeholder, banner upload placeholder, and hex color pickers for primary, secondary, accent, background, and text colors. Persist settings through the API into MSSQL.

## Prompt 5: Onboarding wizard

Build a client onboarding wizard that collects show name, show slug, YouTube channel URL, and initial branding choices. On submit, create a show, create a YouTube source, and create a `YouTubeChannelInventory` processing job.

## Prompt 6: n8n job API

Implement API endpoints for n8n to claim pending jobs, heartbeat running jobs, complete jobs, fail jobs, and write discovered YouTube video metadata. Use a service token or signed request header for n8n authentication.

## Prompt 7: Public portal shell

Build a public portal route that resolves a show from slug or hostname, loads branding, shows the show header, renders a search box, and lists recent processed episodes.

## Prompt 8: Search MVP

Implement a search endpoint that queries Supabase/Postgres full-text search over episode titles, summaries, tags, topics, and transcript chunks. Return episode title, thumbnail, date, summary, tags, top excerpt, timestamp start/end, and relevance score.

