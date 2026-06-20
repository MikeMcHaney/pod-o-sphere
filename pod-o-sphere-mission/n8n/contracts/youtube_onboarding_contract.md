# n8n Contract: YouTube Onboarding

## Purpose

n8n remains behind the curtain and performs source discovery, transcript fetching, and enrichment orchestration.

## Claim pending job

`POST /internal/jobs/claim`

Request:

```json
{
  "workerName": "n8n-youtube-inventory-01",
  "jobTypes": ["YouTubeChannelInventory"],
  "maxJobs": 1
}
```

Response:

```json
{
  "jobId": "uuid",
  "tenantId": "uuid",
  "showId": "uuid",
  "sourceId": "uuid",
  "jobType": "YouTubeChannelInventory",
  "payload": {
    "youtubeChannelUrl": "https://www.youtube.com/@example",
    "mode": "BacklogAndLatest"
  }
}
```

## Write discovered videos

`POST /internal/sources/youtube/videos/upsert`

Request:

```json
{
  "tenantId": "uuid",
  "showId": "uuid",
  "sourceId": "uuid",
  "videos": [
    {
      "platformVideoId": "abc123",
      "url": "https://www.youtube.com/watch?v=abc123",
      "title": "Episode title",
      "description": "Video description",
      "thumbnailUrl": "https://...",
      "publishedAt": "2026-06-19T12:00:00Z",
      "durationSeconds": 3600
    }
  ]
}
```

Response:

```json
{
  "inserted": 10,
  "updated": 2,
  "skipped": 0,
  "transcriptJobsCreated": 12
}
```

## Complete job

`POST /internal/jobs/{jobId}/complete`

Request:

```json
{
  "summary": "Discovered 124 videos and queued transcript jobs.",
  "metrics": {
    "videosDiscovered": 124,
    "transcriptJobsCreated": 124
  }
}
```

## Fail job

`POST /internal/jobs/{jobId}/fail`

Request:

```json
{
  "errorCode": "YOUTUBE_CHANNEL_NOT_FOUND",
  "errorMessage": "Channel could not be resolved.",
  "retryable": false
}
```

