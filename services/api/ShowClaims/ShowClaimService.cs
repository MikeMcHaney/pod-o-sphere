using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PodOSphere.Api.Auditing;
using PodOSphere.Api.Data;
using PodOSphere.Api.Identity;

namespace PodOSphere.Api.ShowClaims;

public sealed record SubmitShowClaimRequest(
    string? ClaimType,
    string? SourceUrl,
    Guid? ShowId = null,
    string? Notes = null);

public sealed record ReviewShowClaimRequest(string? Notes = null);

public sealed record ShowClaimResult(
    Guid ShowClaimId,
    Guid? ShowId,
    Guid RequestingAppUserId,
    Guid? ReviewedByAppUserId,
    string ClaimType,
    string SourceUrl,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed class ShowClaimService(
    IDbContextFactory<MetadataDbContext> dbContextFactory,
    AuditWriter auditWriter)
{
    public async Task<IResult> SubmitAsync(
        ClaimsPrincipal actor,
        SubmitShowClaimRequest request,
        CancellationToken cancellationToken)
    {
        var claimType = NormalizeRequired(request.ClaimType);
        var sourceUrl = NormalizeRequired(request.SourceUrl);
        if (claimType is null || sourceUrl is null)
        {
            return Results.BadRequest("claimType and sourceUrl are required.");
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var appUserId = await GetRegisteredActorAppUserIdAsync(actor, dbContext, cancellationToken);
        if (appUserId is null)
        {
            return Results.Problem("Authenticated identity is not registered in Pod-o-Sphere.", statusCode: StatusCodes.Status403Forbidden);
        }

        if (request.ShowId is { } showId)
        {
            var showExists = await dbContext.Shows.AnyAsync(show => show.ShowId == showId, cancellationToken);
            if (!showExists)
            {
                return Results.NotFound("Show was not found.");
            }
        }

        var showClaim = new ShowClaim
        {
            ShowId = request.ShowId,
            RequestingAppUserId = appUserId.Value,
            ClaimType = claimType,
            SourceUrl = sourceUrl,
            Notes = NormalizeOptional(request.Notes),
            RequestingAppUser = null!
        };

        dbContext.ShowClaims.Add(showClaim);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(
            actor,
            new AuditWriteRequest(
                AuditEventTypes.ShowClaimSubmitted,
                TargetType: nameof(ShowClaim),
                TargetId: showClaim.ShowClaimId.ToString(),
                ShowId: showClaim.ShowId,
                Metadata: new { showClaim.ClaimType, showClaim.SourceUrl }),
            cancellationToken);

        return Results.Ok(ToResult(showClaim));
    }

    public async Task<IResult> GetPendingAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var pendingClaims = await dbContext.ShowClaims
            .AsNoTracking()
            .Where(showClaim => showClaim.Status == "Pending")
            .OrderBy(showClaim => showClaim.CreatedAtUtc)
            .Select(showClaim => new ShowClaimResult(
                showClaim.ShowClaimId,
                showClaim.ShowId,
                showClaim.RequestingAppUserId,
                showClaim.ReviewedByAppUserId,
                showClaim.ClaimType,
                showClaim.SourceUrl,
                showClaim.Status,
                showClaim.Notes,
                showClaim.CreatedAtUtc,
                showClaim.ReviewedAtUtc))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(pendingClaims);
    }

    public Task<IResult> ApproveAsync(
        ClaimsPrincipal actor,
        Guid showClaimId,
        ReviewShowClaimRequest? request,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            actor,
            showClaimId,
            "Approved",
            AuditEventTypes.ShowClaimApproved,
            request,
            cancellationToken);

    public Task<IResult> RejectAsync(
        ClaimsPrincipal actor,
        Guid showClaimId,
        ReviewShowClaimRequest? request,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            actor,
            showClaimId,
            "Rejected",
            AuditEventTypes.ShowClaimRejected,
            request,
            cancellationToken);

    private async Task<IResult> ReviewAsync(
        ClaimsPrincipal actor,
        Guid showClaimId,
        string status,
        string eventType,
        ReviewShowClaimRequest? request,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reviewerAppUserId = await GetRegisteredActorAppUserIdAsync(actor, dbContext, cancellationToken);
        if (reviewerAppUserId is null)
        {
            return Results.Problem("Authenticated identity is not registered in Pod-o-Sphere.", statusCode: StatusCodes.Status403Forbidden);
        }

        var showClaim = await dbContext.ShowClaims.SingleOrDefaultAsync(
            claim => claim.ShowClaimId == showClaimId,
            cancellationToken);
        if (showClaim is null)
        {
            return Results.NotFound();
        }

        if (!showClaim.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only pending show claims can be reviewed.");
        }

        showClaim.Status = status;
        showClaim.ReviewedByAppUserId = reviewerAppUserId.Value;
        showClaim.ReviewedAtUtc = DateTime.UtcNow;
        if (NormalizeOptional(request?.Notes) is { } notes)
        {
            showClaim.Notes = notes;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(
            actor,
            new AuditWriteRequest(
                eventType,
                TargetType: nameof(ShowClaim),
                TargetId: showClaim.ShowClaimId.ToString(),
                ShowId: showClaim.ShowId,
                Metadata: new { showClaim.ClaimType, showClaim.SourceUrl, showClaim.Status }),
            cancellationToken);

        return Results.Ok(ToResult(showClaim));
    }

    private static async Task<Guid?> GetRegisteredActorAppUserIdAsync(
        ClaimsPrincipal actor,
        MetadataDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tokenIdentity = AppIdentityService.GetTokenIdentity(actor);
        if (tokenIdentity is null)
        {
            return null;
        }

        return await dbContext.AppUsers
            .Where(user => user.IdentityIssuer == tokenIdentity.Issuer && user.IdentitySubject == tokenIdentity.Subject)
            .Select(user => (Guid?)user.AppUserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static string? NormalizeRequired(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static ShowClaimResult ToResult(ShowClaim showClaim) => new(
        showClaim.ShowClaimId,
        showClaim.ShowId,
        showClaim.RequestingAppUserId,
        showClaim.ReviewedByAppUserId,
        showClaim.ClaimType,
        showClaim.SourceUrl,
        showClaim.Status,
        showClaim.Notes,
        showClaim.CreatedAtUtc,
        showClaim.ReviewedAtUtc);
}
