namespace PodOSphere.Api.Data;

public sealed class Tenant
{
    public Guid TenantId { get; set; }
    public required string TenantName { get; set; }
    public required string Slug { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<TenantUser> Users { get; set; } = [];
    public ICollection<Show> Shows { get; set; } = [];
    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = [];
    public BillingAccount? BillingAccount { get; set; }
    public ICollection<UsageCounter> UsageCounters { get; set; } = [];
}

public sealed class AppUser
{
    public Guid AppUserId { get; set; }
    public required string IdentityIssuer { get; set; }
    public required string IdentitySubject { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public ICollection<TenantUser> TenantMemberships { get; set; } = [];
}

public sealed class Role
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; }
    public string? Description { get; set; }
    public ICollection<TenantUser> TenantMemberships { get; set; } = [];
}

public sealed class TenantUser
{
    public Guid TenantUserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid AppUserId { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public required Tenant Tenant { get; set; }
    public required AppUser AppUser { get; set; }
    public required Role Role { get; set; }
}

public sealed class Show
{
    public Guid ShowId { get; set; }
    public Guid TenantId { get; set; }
    public required string ShowName { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required Tenant Tenant { get; set; }
    public PortalSettings? PortalSettings { get; set; }
    public ICollection<PortalDomain> Domains { get; set; } = [];
    public ICollection<DataSource> DataSources { get; set; } = [];
    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = [];
    public ICollection<UsageCounter> UsageCounters { get; set; } = [];
}

public sealed class PortalSettings
{
    public Guid PortalSettingsId { get; set; }
    public Guid ShowId { get; set; }
    public string? PortalDisplayName { get; set; }
    public string? LogoBlobUrl { get; set; }
    public string? BannerBlobUrl { get; set; }
    public string PrimaryHex { get; set; } = "#2563EB";
    public string SecondaryHex { get; set; } = "#7C3AED";
    public string AccentHex { get; set; } = "#F97316";
    public string BackgroundHex { get; set; } = "#FFFFFF";
    public string TextHex { get; set; } = "#111827";
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required Show Show { get; set; }
}

public sealed class PortalDomain
{
    public Guid PortalDomainId { get; set; }
    public Guid ShowId { get; set; }
    public required string Hostname { get; set; }
    public string DomainType { get; set; } = "Custom";
    public string VerificationStatus { get; set; } = "Pending";
    public string? VerificationToken { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public required Show Show { get; set; }
}

public sealed class DataSource
{
    public Guid DataSourceId { get; set; }
    public Guid ShowId { get; set; }
    public required string SourceType { get; set; }
    public required string SourceUrl { get; set; }
    public string? ExternalSourceId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? LastInventoryAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required Show Show { get; set; }
    public ICollection<ProcessingJob> ProcessingJobs { get; set; } = [];
}

public sealed class ProcessingJob
{
    public Guid ProcessingJobId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ShowId { get; set; }
    public Guid? DataSourceId { get; set; }
    public required string JobType { get; set; }
    public string Status { get; set; } = "Pending";
    public int Priority { get; set; } = 50;
    public string? PayloadJson { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string? ClaimedBy { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public DateTime? LastHeartbeatAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required Tenant Tenant { get; set; }
    public required Show Show { get; set; }
    public DataSource? DataSource { get; set; }
}

public sealed class BillingAccount
{
    public Guid BillingAccountId { get; set; }
    public Guid TenantId { get; set; }
    public string BillingProvider { get; set; } = "Stripe";
    public string? ProviderCustomerId { get; set; }
    public string? PlanCode { get; set; }
    public string BillingStatus { get; set; } = "Trial";
    public DateTime? CurrentPeriodStartUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required Tenant Tenant { get; set; }
}

public sealed class UsageCounter
{
    public Guid UsageCounterId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ShowId { get; set; }
    public required string UsageMonth { get; set; }
    public int VideosProcessed { get; set; }
    public int TranscriptMinutes { get; set; }
    public int Searches { get; set; }
    public int ChatMessages { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public required Tenant Tenant { get; set; }
    public Show? Show { get; set; }
}
