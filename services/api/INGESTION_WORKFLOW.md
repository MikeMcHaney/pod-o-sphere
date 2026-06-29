# Ingestion Workflow

This is the current ingestion mental model for the local demo path and the near-term n8n/transcript work.

## Actors

- **Admin portal**: creates a demo show/source and queues inventory.
- **API**: owns tenant-aware metadata, processing jobs, internal worker endpoints, and Supabase writes.
- **MSSQL**: source of truth for tenants, shows, data sources, processing jobs, users, roles, and audit-style operational state.
- **Supabase/Postgres**: source of truth for content intelligence rows such as sources, episodes, transcripts, chunks, summaries, tags, topics, and future search/RAG data.
- **n8n**: worker/orchestrator for external fetches, transcript retrieval, enrichment, and local LLM calls.
- **Local LLMs**: summarization/enrichment workers called from n8n after transcript text exists.

## Core Entities

- `Tenants`: customer/account boundary.
- `Shows`: tenant-owned content container. A show may be created by the YouTube demo setup before any deeper onboarding exists.
- `DataSources`: external content source for a show, currently `YouTubeChannel`.
- `ProcessingJobs`: durable work queue in MSSQL.
- `content.sources`: Supabase mirror of a data source for content workflows.
- `content.episodes`: Supabase episode/video metadata.
- `content.transcripts`: future transcript records.
- `content.transcript_segments` and `content.transcript_chunks`: future timestamped/searchable transcript units.

## Job Types

- `YouTubeChannelInventory`: discover videos for a YouTube channel and upsert episode metadata.
- `YouTubeTranscriptIngestion`: retrieve or produce transcript text for a discovered video.

## Current Demo Flow

1. SuperAdmin uses the admin portal source panel.
2. The admin portal calls `POST /api/admin/youtube-demo`.
3. The API creates or reuses:
   - `Shows`
   - `DataSources`
   - `ProcessingJobs` with `JobType = YouTubeChannelInventory`
4. The source is demo-bounded with:
   - `InventoryMode = Demo`
   - `MaxEpisodes = 5`
5. A worker claims the job via `POST /internal/jobs/claim`.
6. The worker discovers or mocks YouTube video metadata.
7. The worker calls `POST /internal/sources/youtube/videos/upsert`.
8. The API writes:
   - Supabase `content.sources`
   - Supabase `content.episodes`
   - MSSQL `YouTubeTranscriptIngestion` jobs for newly inserted videos
9. The worker completes the inventory job via `POST /internal/jobs/{jobId}/complete`.

The smoke-test version of this is documented in `INGESTION_SMOKE.md`.

## Safety Rules Already In Place

- Internal endpoints require `X-PodOSphere-Internal-Token`.
- Demo sources are capped server-side by `DataSources.MaxEpisodes`.
- Duplicate source creation is guarded by `DataSources(ShowId, SourceType, SourceUrl)`.
- Job claims get a 30-minute lease.
- Heartbeats renew the lease.
- Stale `InProgress` jobs are returned to `Pending` during the next claim attempt.
- Failed jobs may be returned to `Pending` when marked retryable and attempts remain.

## Near-Term n8n Workflow

First workflow should be intentionally boring:

1. Manual trigger.
2. Claim one `YouTubeChannelInventory` job.
3. If no job is returned, stop cleanly.
4. Discover videos:
   - first pass can use mock data or a small deterministic fixture
   - next pass can use YouTube Data API or another channel-resolution strategy
5. Upsert discovered videos.
6. Complete the job if upsert succeeds.
7. Fail the job if discovery or upsert fails.

n8n should not complete a job after a failed upsert.

## Transcript Workflow Next

After inventory is proven through n8n:

1. n8n claims `YouTubeTranscriptIngestion`.
2. n8n retrieves or generates transcript text for the video.
3. API receives transcript payload through a new internal transcript endpoint.
4. API writes Supabase:
   - `content.transcripts`
   - `content.transcript_segments`
   - later `content.transcript_chunks`
5. API or n8n queues summarization/enrichment.
6. n8n calls local LLMs for summary, topics, tags, questions, and entities.
7. API writes enrichment output to Supabase content tables.

## Open Decisions

- YouTube discovery method: official YouTube Data API key, RSS-style feed, or another resolver.
- Transcript source: YouTube captions, external transcription provider, local model, or hybrid.
- Transcript payload contract: raw transcript only first, or raw plus segments.
- Local LLM interface: direct Ollama call from n8n, an API endpoint, or a worker abstraction.
- Public portal read path: API-mediated only, or limited direct Supabase reads later.

## Sanity Check

If a demo smoke test succeeds, expected state is:

- MSSQL has a `Show`.
- MSSQL has a `YouTubeChannel` `DataSource`.
- MSSQL has a completed `YouTubeChannelInventory` job.
- Supabase has one `content.sources` row.
- Supabase has one or more `content.episodes` rows.
- MSSQL has pending `YouTubeTranscriptIngestion` jobs for newly inserted episodes.
