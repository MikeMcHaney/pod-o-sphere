-- Pod-o-Sphere Supabase/Postgres content intelligence schema
-- Run through Supabase CLI migrations.

create extension if not exists vector;
create schema if not exists content;

create table if not exists content.shows (
    show_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null unique,
    show_name text not null,
    slug text not null,
    is_published boolean not null default false,
    created_at timestamptz not null default now(),
    updated_at timestamptz
);

create table if not exists content.sources (
    source_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    source_external_id uuid not null unique,
    source_type text not null,
    source_url text not null,
    platform_channel_id text,
    created_at timestamptz not null default now(),
    updated_at timestamptz
);

create table if not exists content.episodes (
    episode_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    source_external_id uuid not null,
    platform text not null default 'YouTube',
    platform_episode_id text not null,
    canonical_url text not null,
    title text not null,
    description text,
    thumbnail_url text,
    published_at timestamptz,
    duration_seconds integer,
    transcript_status text not null default 'Pending',
    enrichment_status text not null default 'Pending',
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    unique (show_external_id, platform, platform_episode_id)
);

create index if not exists ix_episodes_show_published on content.episodes(show_external_id, published_at desc);
create index if not exists ix_episodes_transcript_status on content.episodes(transcript_status);
create index if not exists ix_episodes_enrichment_status on content.episodes(enrichment_status);

create table if not exists content.transcripts (
    transcript_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    provider text not null,
    provider_job_id text,
    language_code text,
    raw_text text not null,
    raw_payload jsonb,
    confidence numeric(5,4),
    created_at timestamptz not null default now(),
    unique (episode_id, provider)
);

create table if not exists content.transcript_segments (
    segment_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    transcript_id uuid not null references content.transcripts(transcript_id) on delete cascade,
    start_seconds numeric(12,3) not null,
    end_seconds numeric(12,3),
    speaker_label text,
    text text not null,
    ordinal integer not null,
    created_at timestamptz not null default now()
);

create index if not exists ix_transcript_segments_episode_ordinal on content.transcript_segments(episode_id, ordinal);

create table if not exists content.transcript_chunks (
    chunk_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    transcript_id uuid not null references content.transcripts(transcript_id) on delete cascade,
    start_seconds numeric(12,3),
    end_seconds numeric(12,3),
    chunk_text text not null,
    token_count integer,
    ordinal integer not null,
    embedding vector(1536),
    search_vector tsvector generated always as (to_tsvector('english', coalesce(chunk_text, ''))) stored,
    created_at timestamptz not null default now()
);

create index if not exists ix_transcript_chunks_episode_ordinal on content.transcript_chunks(episode_id, ordinal);
create index if not exists ix_transcript_chunks_search_vector on content.transcript_chunks using gin(search_vector);
create index if not exists ix_transcript_chunks_embedding on content.transcript_chunks using ivfflat (embedding vector_cosine_ops) with (lists = 100);

create table if not exists content.episode_summaries (
    episode_summary_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    short_summary text,
    long_summary text,
    key_takeaways jsonb,
    key_questions jsonb,
    model_name text,
    prompt_version text,
    created_at timestamptz not null default now(),
    updated_at timestamptz,
    unique (episode_id)
);

create table if not exists content.topics (
    topic_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    topic_name text not null,
    topic_slug text not null,
    description text,
    created_at timestamptz not null default now(),
    unique (show_external_id, topic_slug)
);

create table if not exists content.tags (
    tag_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    tag_name text not null,
    tag_slug text not null,
    created_at timestamptz not null default now(),
    unique (show_external_id, tag_slug)
);

create table if not exists content.episode_topics (
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    topic_id uuid not null references content.topics(topic_id) on delete cascade,
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    relevance_score numeric(6,5) not null default 0,
    evidence jsonb,
    created_at timestamptz not null default now(),
    primary key (episode_id, topic_id)
);

create table if not exists content.episode_tags (
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    tag_id uuid not null references content.tags(tag_id) on delete cascade,
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    relevance_score numeric(6,5) not null default 0,
    created_at timestamptz not null default now(),
    primary key (episode_id, tag_id)
);

create table if not exists content.entities (
    entity_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    entity_name text not null,
    entity_type text not null,
    entity_slug text not null,
    created_at timestamptz not null default now(),
    unique (show_external_id, entity_type, entity_slug)
);

create table if not exists content.episode_entities (
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    entity_id uuid not null references content.entities(entity_id) on delete cascade,
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    mention_count integer not null default 1,
    evidence jsonb,
    created_at timestamptz not null default now(),
    primary key (episode_id, entity_id)
);

create table if not exists content.search_documents (
    search_document_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    document_type text not null, -- Episode, Chunk, Topic, Tag
    title text,
    body text not null,
    metadata jsonb,
    search_vector tsvector generated always as (to_tsvector('english', coalesce(title, '') || ' ' || coalesce(body, ''))) stored,
    created_at timestamptz not null default now()
);

create index if not exists ix_search_documents_show_type on content.search_documents(show_external_id, document_type);
create index if not exists ix_search_documents_search_vector on content.search_documents using gin(search_vector);

create table if not exists content.chat_sessions (
    chat_session_id uuid primary key default gen_random_uuid(),
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    visitor_fingerprint text,
    created_at timestamptz not null default now()
);

create table if not exists content.chat_messages (
    chat_message_id uuid primary key default gen_random_uuid(),
    chat_session_id uuid not null references content.chat_sessions(chat_session_id) on delete cascade,
    tenant_external_id uuid not null,
    show_external_id uuid not null,
    role text not null,
    content text not null,
    citations jsonb,
    created_at timestamptz not null default now()
);

create table if not exists content.transcript_segments (
    id uuid primary key default gen_random_uuid(),
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    segment_index int not null,
    text text not null,
    start_seconds numeric(12,3) not null,
    duration_seconds numeric(12,3) not null,
    end_seconds numeric(12,3) generated always as (start_seconds + duration_seconds) stored,
    created_at timestamptz not null default now(),
    unique (episode_id, segment_index)
);

create table if not exists content.episode_analysis_chunks (
    id uuid primary key default gen_random_uuid(),
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
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

create table if not exists content.topic_occurrences (
    id uuid primary key default gen_random_uuid(),
    topic_id uuid not null references content.topics(topic_id) on delete cascade,
    episode_id uuid not null references content.episodes(episode_id) on delete cascade,
    chunk_id uuid null references content.episode_analysis_chunks(id) on delete set null,
    start_seconds numeric(12,3) not null,
    end_seconds numeric(12,3) not null,
    confidence numeric(5,4) null,
    excerpt text null,
    created_at timestamptz not null default now()
);