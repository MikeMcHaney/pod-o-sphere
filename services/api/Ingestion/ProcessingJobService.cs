using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using PodOSphere.Api.Configuration;
using PodOSphere.Api.Data;

namespace PodOSphere.Api.Ingestion;

public static class IngestionJobTypes
{
    public const string YouTubeChannelInventory = "YouTubeChannelInventory";
    public const string YouTubeTranscriptIngestion = "YouTubeTranscriptIngestion";
}

public static class ProcessingJobStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class DataSourceTypes
{
    public const string YouTubeChannel = "YouTubeChannel";
}

public static class DataSourceStatuses
{
    public const string Pending = "Pending";
    public const string Inventoried = "Inventoried";
}

public static class InventoryModes
{
    public const string Demo = "Demo";
    public const string Full = "Full";
    public const string DemoLatestPayload = "DemoLatest";
    public const string BacklogAndLatestPayload = "BacklogAndLatest";
}

public sealed record CreateYouTubeSourceRequest(
    string SourceUrl,
    string InventoryMode = "Demo",
    int? MaxEpisodes = null);

public sealed record CreateYouTubeDemoRequest(
    Guid TenantId,
    string ShowName,
    string? ShowSlug,
    string SourceUrl,
    string InventoryMode = "Demo",
    int? MaxEpisodes = null);

public sealed record CreateYouTubeSourceResponse(
    Guid TenantId,
    Guid ShowId,
    string ShowName,
    Guid SourceId,
    Guid JobId,
    string SourceUrl,
    string InventoryMode,
    int? MaxEpisodes,
    string JobType);

public sealed record ClaimJobRequest(
    string WorkerName,
    string[]? JobTypes,
    int MaxJobs = 1);

public sealed record ClaimedJobResponse(
    Guid JobId,
    Guid TenantId,
    Guid ShowId,
    Guid? SourceId,
    string JobType,
    JsonElement? Payload);

public sealed record CompleteJobRequest(
    string? Summary = null,
    JsonElement? Metrics = null);

public sealed record FailJobRequest(
    string ErrorCode,
    string ErrorMessage,
    bool Retryable = false);

public sealed record JobStatusResponse(
    Guid JobId,
    string Status,
    int AttemptCount,
    DateTime? LastHeartbeatAtUtc,
    DateTime? CompletedAtUtc);

public sealed record YouTubeVideoUpsertRequest(
    Guid TenantId,
    Guid ShowId,
    Guid SourceId,
    YouTubeVideoUpsertItem[]? Videos);

public sealed record YouTubeVideoUpsertItem(
    string PlatformVideoId,
    string Url,
    string Title,
    string? Description = null,
    string? ThumbnailUrl = null,
    DateTime? PublishedAt = null,
    int? DurationSeconds = null);

public sealed record YouTubeVideoUpsertResponse(
    int Inserted,
    int Updated,
    int Skipped,
    int TranscriptJobsCreated);

public sealed class ProcessingJobService(
    IDbContextFactory<MetadataDbContext> dbContextFactory,
    IOptions<PodOSphereOptions> options)
{
    private static readonly Regex SlugInvalidCharacters = new("[^a-z0-9-]+", RegexOptions.Compiled);
    private static readonly Regex SlugDuplicateHyphens = new("-+", RegexOptions.Compiled);
    private static readonly TimeSpan DefaultJobLease = TimeSpan.FromMinutes(30);

    public async Task<IResult> CreateYouTubeDemoAsync(
        CreateYouTubeDemoRequest request,
        CancellationToken cancellationToken)
    {
        var showName = NormalizeRequired(request.ShowName);
        var sourceUrl = NormalizeRequired(request.SourceUrl);
        if (showName is null || sourceUrl is null || !IsSupportedYouTubeUrl(sourceUrl))
        {
            return Results.BadRequest("showName and a valid YouTube channel URL are required.");
        }

        var sourceSettings = NormalizeSourceSettings(sourceUrl, request.InventoryMode, request.MaxEpisodes);
        if (sourceSettings.ErrorMessage is { } errorMessage)
        {
            return Results.BadRequest(errorMessage);
        }

        await using var strategyContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var response = await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var tenantExists = await dbContext.Tenants.AnyAsync(tenant => tenant.TenantId == request.TenantId, cancellationToken);
            if (!tenantExists)
            {
                return null;
            }

            var slug = await BuildUniqueShowSlugAsync(
                dbContext,
                request.TenantId,
                request.ShowSlug ?? showName,
                cancellationToken);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var show = new Show
            {
                TenantId = request.TenantId,
                ShowName = showName,
                Slug = slug,
            Status = "Draft",
                Tenant = null!
            };
            dbContext.Shows.Add(show);
            await dbContext.SaveChangesAsync(cancellationToken);

            var created = await CreateSourceAndJobAsync(
                dbContext,
                show.TenantId,
                show.ShowId,
                show.ShowName,
                sourceSettings,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return created;
        });

        return response is null ? Results.NotFound("Tenant was not found.") : Results.Ok(response);
    }

    public async Task<IResult> CreateYouTubeSourceAsync(
        Guid showId,
        CreateYouTubeSourceRequest request,
        CancellationToken cancellationToken)
    {
        var sourceUrl = NormalizeRequired(request.SourceUrl);
        if (sourceUrl is null || !IsSupportedYouTubeUrl(sourceUrl))
        {
            return Results.BadRequest("A valid YouTube channel URL is required.");
        }

        var sourceSettings = NormalizeSourceSettings(sourceUrl, request.InventoryMode, request.MaxEpisodes);
        if (sourceSettings.ErrorMessage is { } errorMessage)
        {
            return Results.BadRequest(errorMessage);
        }

        await using var strategyContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var response = await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var show = await dbContext.Shows
                .AsNoTracking()
                .Where(show => show.ShowId == showId)
                .Select(show => new { show.ShowId, show.TenantId, show.ShowName })
                .SingleOrDefaultAsync(cancellationToken);
            if (show is null)
            {
                return null;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var created = await CreateSourceAndJobAsync(
                dbContext,
                show.TenantId,
                show.ShowId,
                show.ShowName,
                sourceSettings,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return created;
        });

        return response is null ? Results.NotFound("Show was not found.") : Results.Ok(response);
    }

    private static async Task<CreateYouTubeSourceResponse> CreateSourceAndJobAsync(
        MetadataDbContext dbContext,
        Guid tenantId,
        Guid showId,
        string showName,
        SourceSettings sourceSettings,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dataSource = new DataSource
        {
            ShowId = showId,
            SourceType = DataSourceTypes.YouTubeChannel,
            SourceUrl = sourceSettings.SourceUrl,
            InventoryMode = sourceSettings.InventoryMode,
            MaxEpisodes = sourceSettings.MaxEpisodes,
            Status = DataSourceStatuses.Pending,
            Show = null!
        };
        var existingSource = await dbContext.DataSources.SingleOrDefaultAsync(
            source =>
                source.ShowId == showId &&
                source.SourceType == DataSourceTypes.YouTubeChannel &&
                source.SourceUrl == sourceSettings.SourceUrl,
            cancellationToken);
        if (existingSource is null)
        {
            dbContext.DataSources.Add(dataSource);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            existingSource.InventoryMode = sourceSettings.InventoryMode;
            existingSource.MaxEpisodes = sourceSettings.MaxEpisodes;
            existingSource.Status = DataSourceStatuses.Pending;
            existingSource.UpdatedAtUtc = now;
            dataSource = existingSource;
        }

        var payload = JsonSerializer.Serialize(
            new
            {
                youtubeChannelUrl = sourceSettings.SourceUrl,
                mode = sourceSettings.InventoryMode == InventoryModes.Demo ? InventoryModes.DemoLatestPayload : InventoryModes.BacklogAndLatestPayload,
                maxEpisodes = sourceSettings.MaxEpisodes
            },
            JsonSerializerOptions.Web);
        var processingJob = new ProcessingJob
        {
            TenantId = tenantId,
            ShowId = showId,
            DataSourceId = dataSource.DataSourceId,
            JobType = IngestionJobTypes.YouTubeChannelInventory,
            Priority = sourceSettings.InventoryMode == InventoryModes.Demo ? 75 : 50,
            PayloadJson = payload,
            CreatedAtUtc = now,
            Tenant = null!,
            Show = null!
        };
        dbContext.ProcessingJobs.Add(processingJob);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateYouTubeSourceResponse(
            tenantId,
            showId,
            showName,
            dataSource.DataSourceId,
            processingJob.ProcessingJobId,
            dataSource.SourceUrl,
            dataSource.InventoryMode,
            dataSource.MaxEpisodes,
            processingJob.JobType);
    }

    public async Task<IResult> ClaimAsync(ClaimJobRequest request, CancellationToken cancellationToken)
    {
        var workerName = NormalizeRequired(request.WorkerName);
        var jobTypes = (request.JobTypes ?? [])
            .Select(NormalizeRequired)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (workerName is null || jobTypes.Length == 0)
        {
            return Results.BadRequest("workerName and at least one jobType are required.");
        }

        await using var strategyContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        var claimedJob = await strategy.ExecuteAsync(async () =>
        {
            var now = DateTime.UtcNow;
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var staleJobs = await dbContext.ProcessingJobs
                .Where(processingJob =>
                    processingJob.Status == ProcessingJobStatuses.InProgress &&
                    processingJob.LeaseExpiresAtUtc != null &&
                    processingJob.LeaseExpiresAtUtc <= now &&
                    processingJob.AttemptCount < processingJob.MaxAttempts)
                .ToArrayAsync(cancellationToken);
            foreach (var staleJob in staleJobs)
            {
                staleJob.Status = ProcessingJobStatuses.Pending;
                staleJob.ClaimedBy = null;
                staleJob.LeaseExpiresAtUtc = null;
                staleJob.UpdatedAtUtc = now;
            }

            var job = await dbContext.ProcessingJobs
                .Where(processingJob =>
                    processingJob.Status == ProcessingJobStatuses.Pending &&
                    processingJob.AttemptCount < processingJob.MaxAttempts &&
                    jobTypes.Contains(processingJob.JobType))
                .OrderByDescending(processingJob => processingJob.Priority)
                .ThenBy(processingJob => processingJob.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (job is null)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return null;
            }

            job.Status = ProcessingJobStatuses.InProgress;
            job.ClaimedBy = workerName;
            job.ClaimedAtUtc = now;
            job.LeaseExpiresAtUtc = now.Add(DefaultJobLease);
            job.LastHeartbeatAtUtc = now;
            job.StartedAtUtc ??= now;
            job.AttemptCount += 1;
            job.UpdatedAtUtc = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ToClaimedJobResponse(job);
        });

        return claimedJob is null ? Results.NoContent() : Results.Ok(claimedJob);
    }

    public async Task<IResult> HeartbeatAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.ProcessingJobs.SingleOrDefaultAsync(
            processingJob => processingJob.ProcessingJobId == jobId,
            cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        if (!job.Status.Equals(ProcessingJobStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only in-progress jobs can receive heartbeats.");
        }

        job.LastHeartbeatAtUtc = DateTime.UtcNow;
        job.LeaseExpiresAtUtc = job.LastHeartbeatAtUtc.Value.Add(DefaultJobLease);
        job.UpdatedAtUtc = job.LastHeartbeatAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToStatusResponse(job));
    }

    public async Task<IResult> CompleteAsync(
        Guid jobId,
        CompleteJobRequest request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.ProcessingJobs.SingleOrDefaultAsync(
            processingJob => processingJob.ProcessingJobId == jobId,
            cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        if (!job.Status.Equals(ProcessingJobStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only in-progress jobs can be completed.");
        }

        var now = DateTime.UtcNow;
        job.Status = ProcessingJobStatuses.Completed;
        job.CompletedAtUtc = now;
        job.LeaseExpiresAtUtc = null;
        job.UpdatedAtUtc = now;
        job.ErrorCode = null;
        job.ErrorMessage = null;
        job.ResultJson = JsonSerializer.Serialize(
            new { request.Summary, request.Metrics },
            JsonSerializerOptions.Web);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToStatusResponse(job));
    }

    public async Task<IResult> FailAsync(
        Guid jobId,
        FailJobRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ErrorCode) || string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            return Results.BadRequest("errorCode and errorMessage are required.");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var job = await dbContext.ProcessingJobs.SingleOrDefaultAsync(
            processingJob => processingJob.ProcessingJobId == jobId,
            cancellationToken);
        if (job is null)
        {
            return Results.NotFound();
        }

        if (!job.Status.Equals(ProcessingJobStatuses.InProgress, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only in-progress jobs can be failed.");
        }

        var retry = request.Retryable && job.AttemptCount < job.MaxAttempts;
        var now = DateTime.UtcNow;
        job.Status = retry ? ProcessingJobStatuses.Pending : ProcessingJobStatuses.Failed;
        job.ClaimedBy = retry ? null : job.ClaimedBy;
        job.LeaseExpiresAtUtc = null;
        job.ErrorCode = request.ErrorCode.Trim();
        job.ErrorMessage = request.ErrorMessage.Trim();
        job.UpdatedAtUtc = now;
        if (!retry)
        {
            job.CompletedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToStatusResponse(job));
    }

    public async Task<IResult> UpsertYouTubeVideosAsync(
        YouTubeVideoUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var videos = (request.Videos ?? [])
            .Where(video =>
                !string.IsNullOrWhiteSpace(video.PlatformVideoId) &&
                !string.IsNullOrWhiteSpace(video.Url) &&
                !string.IsNullOrWhiteSpace(video.Title))
            .ToArray();
        if (videos.Length == 0)
        {
            return Results.BadRequest("At least one valid video is required.");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var source = await dbContext.DataSources
            .AsNoTracking()
            .Include(dataSource => dataSource.Show)
            .Where(dataSource =>
                dataSource.DataSourceId == request.SourceId &&
                dataSource.ShowId == request.ShowId &&
                dataSource.Show.TenantId == request.TenantId &&
                dataSource.SourceType == DataSourceTypes.YouTubeChannel)
            .SingleOrDefaultAsync(cancellationToken);
        if (source is null)
        {
            return Results.NotFound("YouTube data source was not found.");
        }

        var acceptedVideos = source.MaxEpisodes is { } maxEpisodes
            ? videos.Take(maxEpisodes).ToArray()
            : videos;
        var skipped = videos.Length - acceptedVideos.Length;

        var connectionString = options.Value.SupabasePostgresConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Results.Problem("Supabase Postgres connection string is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await UpsertSupabaseSourceAsync(connection, transaction, request, source, cancellationToken);
        var platformIds = acceptedVideos.Select(video => video.PlatformVideoId.Trim()).ToArray();
        var existingIds = await GetExistingPlatformIdsAsync(connection, transaction, request.ShowId, platformIds, cancellationToken);
        foreach (var video in acceptedVideos)
        {
            await UpsertSupabaseEpisodeAsync(connection, transaction, request, video, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        var inserted = acceptedVideos.Count(video => !existingIds.Contains(video.PlatformVideoId.Trim()));
        var updated = acceptedVideos.Length - inserted;
        var transcriptJobsCreated = await CreateTranscriptJobsAsync(dbContext, request, acceptedVideos, existingIds, cancellationToken);
        var dataSource = await dbContext.DataSources.SingleAsync(dataSource => dataSource.DataSourceId == request.SourceId, cancellationToken);
        dataSource.Status = DataSourceStatuses.Inventoried;
        dataSource.LastInventoryAtUtc = DateTime.UtcNow;
        dataSource.UpdatedAtUtc = dataSource.LastInventoryAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new YouTubeVideoUpsertResponse(inserted, updated, skipped, transcriptJobsCreated));
    }

    private static ClaimedJobResponse ToClaimedJobResponse(ProcessingJob job) => new(
        job.ProcessingJobId,
        job.TenantId,
        job.ShowId,
        job.DataSourceId,
        job.JobType,
        ParsePayload(job.PayloadJson));

    private static JobStatusResponse ToStatusResponse(ProcessingJob job) => new(
        job.ProcessingJobId,
        job.Status,
        job.AttemptCount,
        job.LastHeartbeatAtUtc,
        job.CompletedAtUtc);

    private static JsonElement? ParsePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(payloadJson);
    }

    private static string? NormalizeRequired(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool IsSupportedYouTubeUrl(string sourceUrl) =>
        Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
        (uri.Host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.Equals("www.youtube.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.Equals("m.youtube.com", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeInventoryMode(string? inventoryMode) =>
        inventoryMode?.Trim().Equals(InventoryModes.Full, StringComparison.OrdinalIgnoreCase) == true ? InventoryModes.Full : InventoryModes.Demo;

    private static SourceSettings NormalizeSourceSettings(string sourceUrl, string? inventoryMode, int? maxEpisodes)
    {
        var normalizedMode = NormalizeInventoryMode(inventoryMode);
        var normalizedMaxEpisodes = normalizedMode == InventoryModes.Demo
            ? Math.Clamp(maxEpisodes ?? 5, 1, 25)
            : maxEpisodes;
        return normalizedMaxEpisodes is <= 0
            ? new SourceSettings(sourceUrl, normalizedMode, normalizedMaxEpisodes, "maxEpisodes must be greater than zero when provided.")
            : new SourceSettings(sourceUrl, normalizedMode, normalizedMaxEpisodes);
    }

    private static async Task<string> BuildUniqueShowSlugAsync(
        MetadataDbContext dbContext,
        Guid tenantId,
        string seed,
        CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(seed);
        var slug = baseSlug;
        var suffix = 2;
        while (await dbContext.Shows.AnyAsync(show => show.TenantId == tenantId && show.Slug == slug, cancellationToken))
        {
            var suffixText = $"-{suffix}";
            slug = $"{baseSlug[..Math.Min(baseSlug.Length, 120 - suffixText.Length)]}{suffixText}";
            suffix += 1;
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var slug = SlugDuplicateHyphens.Replace(SlugInvalidCharacters.Replace(lower, "-"), "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "show" : slug[..Math.Min(slug.Length, 120)];
    }

    private sealed record SourceSettings(
        string SourceUrl,
        string InventoryMode,
        int? MaxEpisodes,
        string? ErrorMessage = null);

    private static async Task<int> CreateTranscriptJobsAsync(
        MetadataDbContext dbContext,
        YouTubeVideoUpsertRequest request,
        YouTubeVideoUpsertItem[] videos,
        HashSet<string> existingIds,
        CancellationToken cancellationToken)
    {
        var newVideos = videos
            .Where(video => !existingIds.Contains(video.PlatformVideoId.Trim()))
            .ToArray();
        if (newVideos.Length == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var video in newVideos)
        {
            dbContext.ProcessingJobs.Add(new ProcessingJob
            {
                TenantId = request.TenantId,
                ShowId = request.ShowId,
                DataSourceId = request.SourceId,
                JobType = IngestionJobTypes.YouTubeTranscriptIngestion,
                Priority = 60,
                PayloadJson = JsonSerializer.Serialize(
                    new
                    {
                        platform = "YouTube",
                        platformVideoId = video.PlatformVideoId.Trim(),
                        canonicalUrl = video.Url.Trim()
                    },
                    JsonSerializerOptions.Web),
                CreatedAtUtc = now,
                Tenant = null!,
                Show = null!
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return newVideos.Length;
    }

    private static async Task UpsertSupabaseSourceAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        YouTubeVideoUpsertRequest request,
        DataSource source,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into content.sources (
                tenant_external_id,
                show_external_id,
                source_external_id,
                source_type,
                source_url,
                updated_at)
            values (
                @tenantId,
                @showId,
                @sourceId,
                @sourceType,
                @sourceUrl,
                now())
            on conflict (source_external_id) do update
            set source_type = excluded.source_type,
                source_url = excluded.source_url,
                updated_at = now();
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("tenantId", request.TenantId);
        command.Parameters.AddWithValue("showId", request.ShowId);
        command.Parameters.AddWithValue("sourceId", request.SourceId);
        command.Parameters.AddWithValue("sourceType", source.SourceType);
        command.Parameters.AddWithValue("sourceUrl", source.SourceUrl);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> GetExistingPlatformIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid showId,
        string[] platformIds,
        CancellationToken cancellationToken)
    {
        if (platformIds.Length == 0)
        {
            return [];
        }

        const string sql = """
            select platform_episode_id
            from content.episodes
            where show_external_id = @showId
              and platform = 'YouTube'
              and platform_episode_id = any(@platformIds);
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("showId", showId);
        command.Parameters.AddWithValue("platformIds", platformIds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var existingIds = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            existingIds.Add(reader.GetString(0));
        }

        return existingIds;
    }

    private static async Task UpsertSupabaseEpisodeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        YouTubeVideoUpsertRequest request,
        YouTubeVideoUpsertItem video,
        CancellationToken cancellationToken)
    {
        const string sql = """
            insert into content.episodes (
                tenant_external_id,
                show_external_id,
                source_external_id,
                platform,
                platform_episode_id,
                canonical_url,
                title,
                description,
                thumbnail_url,
                published_at,
                duration_seconds,
                updated_at)
            values (
                @tenantId,
                @showId,
                @sourceId,
                'YouTube',
                @platformEpisodeId,
                @canonicalUrl,
                @title,
                @description,
                @thumbnailUrl,
                @publishedAt,
                @durationSeconds,
                now())
            on conflict (show_external_id, platform, platform_episode_id) do update
            set source_external_id = excluded.source_external_id,
                canonical_url = excluded.canonical_url,
                title = excluded.title,
                description = excluded.description,
                thumbnail_url = excluded.thumbnail_url,
                published_at = excluded.published_at,
                duration_seconds = excluded.duration_seconds,
                updated_at = now();
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("tenantId", request.TenantId);
        command.Parameters.AddWithValue("showId", request.ShowId);
        command.Parameters.AddWithValue("sourceId", request.SourceId);
        command.Parameters.AddWithValue("platformEpisodeId", video.PlatformVideoId.Trim());
        command.Parameters.AddWithValue("canonicalUrl", video.Url.Trim());
        command.Parameters.AddWithValue("title", video.Title.Trim());
        command.Parameters.AddWithValue("description", (object?)video.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("thumbnailUrl", (object?)video.ThumbnailUrl?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("publishedAt", (object?)video.PublishedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("durationSeconds", (object?)video.DurationSeconds ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
