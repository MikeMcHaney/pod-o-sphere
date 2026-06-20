# 02 Phase Plan

## Phase 0: Repo and foundations

Goal: Establish a clean monorepo and infrastructure skeleton.

Deliverables:

- Monorepo structure.
- Local env templates.
- MSSQL schema scripts.
- Supabase migration folder.
- .NET API skeleton.
- React/Next.js app skeletons.
- n8n workflow contract docs.
- Basic CI checks.

Codex tasks:

- Create solution structure.
- Add placeholder apps and services.
- Add typed config model.
- Add health check endpoints.
- Add database migration instructions.

## Phase 1: Identity, tenancy, and admin shell

Goal: Login and tenant-aware administration.

Deliverables:

- Entra ID login.
- Role model for Super Admin and Client Admin.
- Tenant, show, portal, domain, and branding tables.
- Admin layout with navigation.
- Client branding settings page.
- Hex color pickers for primary, secondary, accent, background, text.
- Logo/banner upload placeholders.

Codex tasks:

- Implement Entra auth in admin app and API.
- Add `/me` endpoint with roles and tenant claims.
- Build admin pages for tenants, shows, and portal branding.
- Wire branding save/load APIs.

## Phase 2: Client onboarding and YouTube source setup

Goal: A client can create a show and submit a YouTube channel.

Deliverables:

- Client onboarding wizard, subscribe via Stripe, offer promotions some day (first three months free).
- Source table for YouTube channel.
- Job creation in MSSQL.
- n8n job contract.
- Super Admin job monitor.

Codex tasks:

- Build onboarding form.
- Validate YouTube channel URL.
- Create source record.
- Create onboarding job.
- Add job status screen.

## Phase 3: YouTube inventory collector

Goal: Discover and store videos from a channel.

Deliverables:

- n8n workflow to fetch channel video list.
- Episode/video metadata stored in Supabase.
- MSSQL processing job status updates.
- Duplicate detection by platform video ID.

Codex tasks:

- Define API endpoint for n8n to claim pending jobs.
- Define API endpoint for n8n to write discovered videos.
- Add Supabase episode/video upsert SQL.
- Add tests for duplicate source IDs.

## Phase 4: Transcript ingestion

Goal: Retrieve transcripts for discovered videos.

Deliverables:

- Transcript job queue.
- Transcript provider abstraction.
- Raw transcript storage, azure blob as necessary.
- Timestamp segment storage.
- Failure/retry tracking.

Codex tasks:

- Define transcript provider interface.
- Build API endpoint to create transcript records.
- Store provider metadata and confidence.
- Mark video processing status, In Progress / % complete / transcript-ready etc.

## Phase 5: Content intelligence enrichment

Goal: Create useful structured intelligence from transcripts.

Deliverables:

- Episode summary.
- Short teaser summary.
- Topics and tags.
- Timestamps in relation to tags and topics for deeplinking back to YouTube.
- Key questions.
- Named entities.
- Segment-level relevance.
- Embeddings for transcript chunks.

Codex tasks:

- Add enrichment job type.
- Add prompt contracts.
- Add structured JSON schema validation.
- Add Supabase tables for topics, tags, episode topics, episode tags, chunks, and embeddings.

## Phase 6: Public portal MVP

Goal: Audience-facing searchable portal.

Deliverables:

- Portal route by show slug/domain.
- Brand theme loading.
- Hero banner/logo/color scheme.
- Episode search.
- Tag filter.
- Topic filter.
- Episode cards with thumbnails.
- Search result excerpts with timestamps.

Codex tasks:

- Build public portal UI.
- Build search API.
- Implement full-text search against Supabase/Postgres.
- Return episode cards, tags, excerpts, and timestamps.

## Phase 7: Topic timelines and data visualization

Goal: Show how themes evolve over time.

Deliverables:

- Topic frequency over time.
- Episode timeline.
- Tag cloud.
- Guest/entity explorer.
- Topic detail pages.

Codex tasks:

- Build aggregation views in Supabase.
- Add API endpoints for charts.
- Add React visualizations.

## Phase 8: RAG chatbot beta

Goal: Let visitors ask questions grounded in the show's archive.

Deliverables:

- Retrieval endpoint.
- Chat session table.
- Citation-bearing answers.
- Episode/timestamp citations.
- Rate limiting and abuse controls.

Codex tasks:

- Implement vector/hybrid retrieval.
- Add answer generation prompt with citations required.
- Store chat queries and answer metadata.

## Phase 9: Billing and packaging

Goal: Convert usage into paid tiers.

Deliverables:

- Billing provider integration.
- Plan limits.
- Usage metering.
- Backlog processing limits.
- Monthly enrichment quotas.

Codex tasks:

- Add plan tables.
- Track videos processed, transcript minutes, searches, chat messages, and storage usage.
- Add billing status gates.

