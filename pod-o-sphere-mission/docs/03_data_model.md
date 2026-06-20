# 03 Data Model

## MSSQL ownership

MSSQL stores SaaS operational records:

- Tenants
- Users
- Roles
- Tenant membership
- Billing customers/subscriptions
- Shows
- Portal settings
- Domains
- Data sources
- Processing jobs
- Provider credentials references
- Audit logs

## Supabase/Postgres ownership

Supabase stores content intelligence records:

- Shows mirrored from MSSQL by external ID
- Sources mirrored from MSSQL by external ID
- Episodes/videos
- Transcripts
- Transcript segments
- Transcript chunks
- Topics
- Tags
- Entities
- Episode summaries
- Episode-topic relationships
- Episode-tag relationships
- Embeddings
- Search documents
- Chat sessions/messages later

## Cross-database key rule

MSSQL primary IDs remain the SaaS authority.
Supabase should store external IDs from MSSQL as UUIDs or strings but should not own billing/identity authority.

Example:

- MSSQL `Shows.ShowId` = `uniqueidentifier`
- Supabase `content.shows.external_show_id` = `uuid`

## Tenant isolation

Every Supabase content table should include:

- `tenant_external_id uuid not null`
- `show_external_id uuid not null`

This makes row filtering, backfills, deletion, and export simpler.

## Search design

MVP search can use Postgres full-text search:

- `to_tsvector` on transcript chunks, titles, descriptions, tags, topics.
- `ts_rank` for relevance.

Later search can become hybrid:

- Full-text relevance
- Vector similarity
- Freshness boost
- Tag/topic facet boost
- Exact phrase boost

## Storage design

Object storage should hold large raw payloads when needed:

- Original transcript JSON
- Provider response JSON
- Thumbnails if proxied/cached
- Uploaded brand logos/banners

Small structured transcript segments can remain in Supabase rows.

Some pseudo table structure to consider
create table transcript_segments (
    id uuid primary key default gen_random_uuid(),
    episode_id uuid not null references episodes(id) on delete cascade,
    segment_index int not null,
    text text not null,
    start_seconds numeric(12,3) not null,
    duration_seconds numeric(12,3) not null,
    end_seconds numeric(12,3) generated always as (start_seconds + duration_seconds) stored,
    created_at timestamptz not null default now(),
    unique (episode_id, segment_index)
);

create table episode_analysis_chunks (
    id uuid primary key default gen_random_uuid(),
    episode_id uuid not null references episodes(id) on delete cascade,
    chunk_index int not null,
    start_seconds numeric(12,3) not null,
    end_seconds numeric(12,3) not null,
    text text not null,
    token_estimate int null,
    embedding vector(1536) null,
    search_vector tsvector generated always as (to_tsvector('english', coalesce(text, ''))) stored,
    created_at timestamptz not null default now(),
    unique (episode_id, chunk_index)
);

create table topic_occurrences (
    id uuid primary key default gen_random_uuid(),
    topic_id uuid not null references topics(id) on delete cascade,
    episode_id uuid not null references episodes(id) on delete cascade,
    chunk_id uuid null references episode_analysis_chunks(id) on delete set null,
    start_seconds numeric(12,3) not null,
    end_seconds numeric(12,3) not null,
    confidence numeric(5,4) null,
    excerpt text null,
    created_at timestamptz not null default now()
);
