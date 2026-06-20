# 04 Onboarding Workflow

## Client onboarding flow

1. Visitor lands on brochure site.
2. Visitor starts trial or demo setup - 5 show maximum for trial users. Subscriptions via Stripe and account state handling in MSSQL.
3. Visitor signs in with Entra-backed identity flow.
4. System creates tenant and first client owner.
5. Client creates show profile.
6. Client enters YouTube channel URL.
7. Client picks initial portal slug.
8. Client configures brand colors, logo, and banner.
9. API creates source record and onboarding job.
10. n8n claims onboarding job.
11. n8n discovers channel videos.
12. n8n queues transcript jobs.
13. Transcript jobs complete and enrichment begins.
14. Client sees progress dashboard.
15. Public portal becomes publishable once enough data exists.

## Job status model

Recommended statuses:

- `Pending`
- `Claimed`
- `Running`
- `WaitingOnProvider`
- `Succeeded`
- `Failed`
- `Cancelled`
- `RetryScheduled`

## Job types

- `YouTubeChannelInventory`
- `TranscriptFetch`
- `TranscriptChunking`
- `EpisodeSummary`
- `TopicExtraction`
- `TagExtraction`
- `EntityExtraction`
- `EmbeddingGeneration`
- `SearchIndexRefresh`

## Job priority

Backlog jobs should run at lower priority than recent episode jobs.

Suggested priorities:

- `100`: urgent/manual retry
- `75`: latest episode
- `50`: normal onboarding
- `25`: backlog sweep
- `10`: re-enrichment / experimental

