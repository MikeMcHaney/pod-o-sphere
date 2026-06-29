# Ingestion Smoke Test

Use this after creating a demo show/source from the admin portal. The claim response gives the IDs needed by the video upsert call.

Set local shell values:

```bash
export API_URL="http://localhost:5000"
export PODOSPHERE_INTERNAL_TOKEN="<local-ingestion-token>"
```

Claim the next YouTube inventory job:

```bash
curl -sS -X POST "$API_URL/internal/jobs/claim" \
  -H "Content-Type: application/json" \
  -H "X-PodOSphere-Internal-Token: $PODOSPHERE_INTERNAL_TOKEN" \
  -d '{
    "workerName": "local-smoke",
    "jobTypes": ["YouTubeChannelInventory"],
    "maxJobs": 1
  }'
```

Copy `jobId`, `tenantId`, `showId`, and `sourceId` from that response:

```bash
export JOB_ID="<jobId>"
export TENANT_ID="<tenantId>"
export SHOW_ID="<showId>"
export SOURCE_ID="<sourceId>"
```

Optionally renew the job lease:

```bash
curl -sS -X POST "$API_URL/internal/jobs/$JOB_ID/heartbeat" \
  -H "X-PodOSphere-Internal-Token: $PODOSPHERE_INTERNAL_TOKEN"
```

Upsert demo video metadata. Demo sources are capped by `DataSources.MaxEpisodes`, so extra videos are skipped server-side.

```bash
curl -sS -X POST "$API_URL/internal/sources/youtube/videos/upsert" \
  -H "Content-Type: application/json" \
  -H "X-PodOSphere-Internal-Token: $PODOSPHERE_INTERNAL_TOKEN" \
  -d "{
    \"tenantId\": \"$TENANT_ID\",
    \"showId\": \"$SHOW_ID\",
    \"sourceId\": \"$SOURCE_ID\",
    \"videos\": [
      {
        \"platformVideoId\": \"smoke-001\",
        \"url\": \"https://www.youtube.com/watch?v=smoke001\",
        \"title\": \"Smoke Test Episode 1\",
        \"description\": \"Synthetic local smoke-test episode.\",
        \"thumbnailUrl\": \"https://i.ytimg.com/vi/smoke001/hqdefault.jpg\",
        \"publishedAt\": \"2026-01-01T12:00:00Z\",
        \"durationSeconds\": 1800
      },
      {
        \"platformVideoId\": \"smoke-002\",
        \"url\": \"https://www.youtube.com/watch?v=smoke002\",
        \"title\": \"Smoke Test Episode 2\",
        \"description\": \"Synthetic local smoke-test episode.\",
        \"thumbnailUrl\": \"https://i.ytimg.com/vi/smoke002/hqdefault.jpg\",
        \"publishedAt\": \"2026-01-02T12:00:00Z\",
        \"durationSeconds\": 2100
      }
    ]
  }"
```

Complete the claimed inventory job:

```bash
curl -sS -X POST "$API_URL/internal/jobs/$JOB_ID/complete" \
  -H "Content-Type: application/json" \
  -H "X-PodOSphere-Internal-Token: $PODOSPHERE_INTERNAL_TOKEN" \
  -d '{
    "summary": "Smoke test upserted demo videos.",
    "metrics": {
      "videosDiscovered": 2
    }
  }'
```

If the worker path fails, mark the job failed:

```bash
curl -sS -X POST "$API_URL/internal/jobs/$JOB_ID/fail" \
  -H "Content-Type: application/json" \
  -H "X-PodOSphere-Internal-Token: $PODOSPHERE_INTERNAL_TOKEN" \
  -d '{
    "errorCode": "SMOKE_TEST_FAILED",
    "errorMessage": "Local smoke test failed.",
    "retryable": true
  }'
```

Expected result: Supabase `content.sources` and `content.episodes` receive rows, MSSQL `ProcessingJobs` marks the inventory job completed, and new `YouTubeTranscriptIngestion` jobs are created for newly inserted videos.
