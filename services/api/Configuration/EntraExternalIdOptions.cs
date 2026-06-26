namespace PodOSphere.Api.Configuration;

public sealed class EntraExternalIdOptions
{
    public const string SectionName = "EntraExternalId";

    public string Authority { get; init; } = string.Empty;

    public string TenantId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string GetTokenAuthority()
    {
        var authority = Authority.TrimEnd('/');
        if (authority.EndsWith("/v2.0", StringComparison.OrdinalIgnoreCase))
        {
            return authority;
        }

        return authority.EndsWith($"/{TenantId}", StringComparison.OrdinalIgnoreCase)
            ? $"{authority}/v2.0"
            : $"{authority}/{TenantId}/v2.0";
    }

    public IEnumerable<string> GetValidAudiences()
    {
        return new[] { ClientId, Audience }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
