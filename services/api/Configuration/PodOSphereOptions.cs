namespace PodOSphere.Api.Configuration;

public sealed class PodOSphereOptions
{
    public const string SectionName = "PodOSphere";

    public string MssqlConnectionString { get; init; } = string.Empty;

    public string SupabaseUrl { get; init; } = string.Empty;

    public string SupabaseServiceRoleKey { get; init; } = string.Empty;
}

