using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PodOSphere.Api.Data;

namespace PodOSphere.Api.Identity;

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string ContentManager = "ContentManager";
}

public static class AppPolicies
{
    public const string RequireSuperAdmin = "RequireSuperAdmin";
}

public sealed record TokenIdentity(
    string Issuer,
    string Subject,
    string? Name,
    string? Email,
    string? PreferredUsername);

public sealed record CurrentUserResponse(
    Guid AppUserId,
    string Subject,
    string Issuer,
    string? Name,
    string? ContactEmail,
    string? PreferredUsername,
    bool IsSuperAdmin,
    string[] Roles,
    string[] PlatformRoles,
    TenantMembershipResponse[] TenantMemberships);

public sealed record TenantMembershipResponse(
    Guid TenantId,
    string TenantName,
    string TenantSlug,
    string RoleName,
    bool IsActive);

public sealed record AdminTenantResponse(
    Guid TenantId,
    string TenantName,
    string Slug,
    string Status,
    int ActiveUserCount);

public sealed class SuperAdminRequirement : IAuthorizationRequirement;

public sealed class SuperAdminAuthorizationHandler(AppIdentityService identityService)
    : AuthorizationHandler<SuperAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SuperAdminRequirement requirement)
    {
        if (await identityService.IsSuperAdminAsync(context.User))
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class AppIdentityService(IDbContextFactory<MetadataDbContext> dbContextFactory)
{
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var tokenIdentity = GetTokenIdentity(principal);
        if (tokenIdentity is null)
        {
            return null;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var appUser = await dbContext.AppUsers
            .AsNoTracking()
            .Include(user => user.TenantMemberships)
                .ThenInclude(membership => membership.Tenant)
            .Include(user => user.TenantMemberships)
                .ThenInclude(membership => membership.Role)
            .Include(user => user.PlatformRoleMemberships)
                .ThenInclude(membership => membership.PlatformRole)
            .Where(user =>
                user.IdentityIssuer == tokenIdentity.Issuer &&
                user.IdentitySubject == tokenIdentity.Subject)
            .SingleOrDefaultAsync(cancellationToken);

        if (appUser is null)
        {
            return null;
        }

        var memberships = appUser.TenantMemberships
            .Where(membership => membership.IsActive)
            .Select(membership => new TenantMembershipResponse(
                membership.TenantId,
                membership.Tenant.TenantName,
                membership.Tenant.Slug,
                membership.Role.RoleName,
                membership.IsActive))
            .ToArray();

        var roles = memberships
            .Select(membership => membership.RoleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var platformRoles = appUser.PlatformRoleMemberships
            .Where(membership => membership.IsActive)
            .Select(membership => membership.PlatformRole.RoleName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var allRoles = platformRoles
            .Concat(roles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CurrentUserResponse(
            appUser.AppUserId,
            tokenIdentity.Subject,
            tokenIdentity.Issuer,
            appUser.DisplayName ?? tokenIdentity.Name,
            appUser.ContactEmail,
            appUser.PreferredUsername ?? tokenIdentity.PreferredUsername,
            platformRoles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase),
            allRoles,
            platformRoles,
            memberships);
    }

    public async Task<bool> IsSuperAdminAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var tokenIdentity = GetTokenIdentity(principal);
        if (tokenIdentity is null)
        {
            return false;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.PlatformUserRoles
            .AsNoTracking()
            .AnyAsync(membership =>
                membership.IsActive &&
                membership.AppUser.IdentityIssuer == tokenIdentity.Issuer &&
                membership.AppUser.IdentitySubject == tokenIdentity.Subject &&
                membership.PlatformRole.RoleName == AppRoles.SuperAdmin,
                cancellationToken);
    }

    public static TokenIdentity? GetTokenIdentity(ClaimsPrincipal principal)
    {
        var issuer = principal.FindFirstValue("iss");
        var subject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        return new TokenIdentity(
            issuer,
            subject,
            principal.FindFirstValue("name"),
            principal.FindFirstValue("email") ?? principal.FindFirstValue("emails"),
            principal.FindFirstValue("preferred_username"));
    }
}
