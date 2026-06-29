using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PodOSphere.Api.Data;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "dbo");
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.TenantId).HasDefaultValueSql("newid()");
        builder.Property(x => x.TenantName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Active").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers", "dbo");
        builder.HasKey(x => x.AppUserId);
        builder.Property(x => x.AppUserId).HasDefaultValueSql("newid()");
        builder.Property(x => x.IdentityIssuer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IdentitySubject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.PreferredUsername).HasMaxLength(320);
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.IdentityIssuer, x.IdentitySubject })
            .IsUnique()
            .HasDatabaseName("UQ_AppUsers_Identity_Issuer_Subject");
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo");
        builder.HasKey(x => x.RoleId);
        builder.Property(x => x.RoleId).UseIdentityColumn();
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.RoleName).IsUnique();
    }
}

public sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        builder.ToTable("PlatformRoles", "dbo");
        builder.HasKey(x => x.PlatformRoleId);
        builder.Property(x => x.PlatformRoleId).UseIdentityColumn();
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.RoleName).IsUnique();
    }
}

public sealed class PlatformUserRoleConfiguration : IEntityTypeConfiguration<PlatformUserRole>
{
    public void Configure(EntityTypeBuilder<PlatformUserRole> builder)
    {
        builder.ToTable("PlatformUserRoles", "dbo");
        builder.HasKey(x => x.PlatformUserRoleId);
        builder.Property(x => x.PlatformUserRoleId).HasDefaultValueSql("newid()");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.AppUserId, x.PlatformRoleId })
            .IsUnique()
            .HasDatabaseName("UQ_PlatformUserRoles_User_Role");
        builder.HasOne(x => x.AppUser).WithMany(x => x.PlatformRoleMemberships).HasForeignKey(x => x.AppUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.PlatformRole).WithMany(x => x.UserMemberships).HasForeignKey(x => x.PlatformRoleId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
    public void Configure(EntityTypeBuilder<TenantUser> builder)
    {
        builder.ToTable("TenantUsers", "dbo");
        builder.HasKey(x => x.TenantUserId);
        builder.Property(x => x.TenantUserId).HasDefaultValueSql("newid()");
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.TenantId, x.AppUserId, x.RoleId })
            .IsUnique()
            .HasDatabaseName("UQ_TenantUsers_Tenant_User_Role");
        builder.HasOne(x => x.Tenant).WithMany(x => x.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.AppUser).WithMany(x => x.TenantMemberships).HasForeignKey(x => x.AppUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Role).WithMany(x => x.TenantMemberships).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ShowConfiguration : IEntityTypeConfiguration<Show>
{
    public void Configure(EntityTypeBuilder<Show> builder)
    {
        builder.ToTable("Shows", "dbo");
        builder.HasKey(x => x.ShowId);
        builder.Property(x => x.ShowId).HasDefaultValueSql("newid()");
        builder.Property(x => x.ShowName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Draft").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique().HasDatabaseName("UQ_Shows_Tenant_Slug");
        builder.HasOne(x => x.Tenant).WithMany(x => x.Shows).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class PortalSettingsConfiguration : IEntityTypeConfiguration<PortalSettings>
{
    private const string HexCheck = "like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'";

    public void Configure(EntityTypeBuilder<PortalSettings> builder)
    {
        builder.ToTable("PortalSettings", "dbo", table =>
        {
            table.HasCheckConstraint("CK_PortalSettings_PrimaryHex", $"PrimaryHex {HexCheck}");
            table.HasCheckConstraint("CK_PortalSettings_SecondaryHex", $"SecondaryHex {HexCheck}");
            table.HasCheckConstraint("CK_PortalSettings_AccentHex", $"AccentHex {HexCheck}");
            table.HasCheckConstraint("CK_PortalSettings_BackgroundHex", $"BackgroundHex {HexCheck}");
            table.HasCheckConstraint("CK_PortalSettings_TextHex", $"TextHex {HexCheck}");
        });
        builder.HasKey(x => x.PortalSettingsId);
        builder.Property(x => x.PortalSettingsId).HasDefaultValueSql("newid()");
        builder.Property(x => x.PortalDisplayName).HasMaxLength(200);
        builder.Property(x => x.LogoBlobUrl).HasMaxLength(1000);
        builder.Property(x => x.BannerBlobUrl).HasMaxLength(1000);
        ConfigureHex(builder.Property(x => x.PrimaryHex), "#2563EB");
        ConfigureHex(builder.Property(x => x.SecondaryHex), "#7C3AED");
        ConfigureHex(builder.Property(x => x.AccentHex), "#F97316");
        ConfigureHex(builder.Property(x => x.BackgroundHex), "#FFFFFF");
        ConfigureHex(builder.Property(x => x.TextHex), "#111827");
        builder.Property(x => x.IsPublished).HasDefaultValue(false);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => x.ShowId).IsUnique();
        builder.HasOne(x => x.Show).WithOne(x => x.PortalSettings).HasForeignKey<PortalSettings>(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
    }

    private static void ConfigureHex(PropertyBuilder<string> property, string defaultValue)
    {
        property.HasColumnType("char(7)").IsFixedLength().HasDefaultValue(defaultValue).IsRequired();
    }
}

public sealed class PortalDomainConfiguration : IEntityTypeConfiguration<PortalDomain>
{
    public void Configure(EntityTypeBuilder<PortalDomain> builder)
    {
        builder.ToTable("PortalDomains", "dbo");
        builder.HasKey(x => x.PortalDomainId);
        builder.Property(x => x.PortalDomainId).HasDefaultValueSql("newid()");
        builder.Property(x => x.Hostname).HasMaxLength(255).IsRequired();
        builder.Property(x => x.DomainType).HasMaxLength(50).HasDefaultValue("Custom").IsRequired();
        builder.Property(x => x.VerificationStatus).HasMaxLength(50).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.VerificationToken).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => x.Hostname).IsUnique();
        builder.HasOne(x => x.Show).WithMany(x => x.Domains).HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.ToTable("DataSources", "dbo");
        builder.HasKey(x => x.DataSourceId);
        builder.Property(x => x.DataSourceId).HasDefaultValueSql("newid()");
        builder.Property(x => x.SourceType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExternalSourceId).HasMaxLength(200);
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.InventoryMode).HasMaxLength(50).HasDefaultValue("Full").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.ShowId, x.SourceType, x.SourceUrl }).IsUnique().HasDatabaseName("UQ_DataSources_Show_Type_Url");
        builder.HasOne(x => x.Show).WithMany(x => x.DataSources).HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("ProcessingJobs", "dbo");
        builder.HasKey(x => x.ProcessingJobId);
        builder.Property(x => x.ProcessingJobId).HasDefaultValueSql("newid()");
        builder.Property(x => x.JobType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.Priority).HasDefaultValue(50);
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ResultJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AttemptCount).HasDefaultValue(0);
        builder.Property(x => x.MaxAttempts).HasDefaultValue(3);
        builder.Property(x => x.ClaimedBy).HasMaxLength(200);
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.Status, x.Priority, x.CreatedAtUtc }).HasDatabaseName("IX_ProcessingJobs_Status_Priority").IsDescending(false, true, false);
        builder.HasIndex(x => new { x.Status, x.LeaseExpiresAtUtc }).HasDatabaseName("IX_ProcessingJobs_LeaseExpiry").HasFilter("[Status] = 'InProgress'");
        builder.HasIndex(x => x.ShowId).HasDatabaseName("IX_ProcessingJobs_ShowId");
        builder.HasOne(x => x.Tenant).WithMany(x => x.ProcessingJobs).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Show).WithMany(x => x.ProcessingJobs).HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.DataSource).WithMany(x => x.ProcessingJobs).HasForeignKey(x => x.DataSourceId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class BillingAccountConfiguration : IEntityTypeConfiguration<BillingAccount>
{
    public void Configure(EntityTypeBuilder<BillingAccount> builder)
    {
        builder.ToTable("BillingAccounts", "dbo");
        builder.HasKey(x => x.BillingAccountId);
        builder.Property(x => x.BillingAccountId).HasDefaultValueSql("newid()");
        builder.Property(x => x.BillingProvider).HasMaxLength(50).HasDefaultValue("Stripe").IsRequired();
        builder.Property(x => x.ProviderCustomerId).HasMaxLength(200);
        builder.Property(x => x.PlanCode).HasMaxLength(100);
        builder.Property(x => x.BillingStatus).HasMaxLength(50).HasDefaultValue("Trial").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasOne(x => x.Tenant).WithOne(x => x.BillingAccount).HasForeignKey<BillingAccount>(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class UsageCounterConfiguration : IEntityTypeConfiguration<UsageCounter>
{
    public void Configure(EntityTypeBuilder<UsageCounter> builder)
    {
        builder.ToTable("UsageCounters", "dbo");
        builder.HasKey(x => x.UsageCounterId);
        builder.Property(x => x.UsageCounterId).HasDefaultValueSql("newid()");
        builder.Property(x => x.UsageMonth).HasColumnType("char(7)").IsFixedLength().IsRequired();
        builder.Property(x => x.VideosProcessed).HasDefaultValue(0);
        builder.Property(x => x.TranscriptMinutes).HasDefaultValue(0);
        builder.Property(x => x.Searches).HasDefaultValue(0);
        builder.Property(x => x.ChatMessages).HasDefaultValue(0);
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.TenantId, x.ShowId, x.UsageMonth }).IsUnique().HasDatabaseName("UQ_UsageCounters_Tenant_Show_Month");
        builder.HasOne(x => x.Tenant).WithMany(x => x.UsageCounters).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Show).WithMany(x => x.UsageCounters).HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations", "dbo");
        builder.HasKey(x => x.InvitationId);
        builder.Property(x => x.InvitationId).HasDefaultValueSql("newid()");
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.InvitationTokenHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => x.InvitationTokenHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Email, x.Status }).HasDatabaseName("IX_Invitations_Tenant_Email_Status");
        builder.HasOne(x => x.Tenant).WithMany(x => x.Invitations).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Show).WithMany().HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.CreatedByAppUser).WithMany(x => x.CreatedInvitations).HasForeignKey(x => x.CreatedByAppUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.AcceptedByAppUser).WithMany(x => x.AcceptedInvitations).HasForeignKey(x => x.AcceptedByAppUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ShowClaimConfiguration : IEntityTypeConfiguration<ShowClaim>
{
    public void Configure(EntityTypeBuilder<ShowClaim> builder)
    {
        builder.ToTable("ShowClaims", "dbo");
        builder.HasKey(x => x.ShowClaimId);
        builder.Property(x => x.ShowClaimId).HasDefaultValueSql("newid()");
        builder.Property(x => x.ClaimType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.VerificationTokenHash).HasMaxLength(200);
        builder.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("Pending").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.RequestingAppUserId, x.Status }).HasDatabaseName("IX_ShowClaims_RequestingUser_Status");
        builder.HasIndex(x => new { x.ShowId, x.Status }).HasDatabaseName("IX_ShowClaims_Show_Status");
        builder.HasOne(x => x.Show).WithMany(x => x.Claims).HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.RequestingAppUser).WithMany().HasForeignKey(x => x.RequestingAppUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ReviewedByAppUser).WithMany(x => x.ReviewedShowClaims).HasForeignKey(x => x.ReviewedByAppUserId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents", "dbo");
        builder.HasKey(x => x.AuditEventId);
        builder.Property(x => x.AuditEventId).HasDefaultValueSql("newid()");
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TargetType).HasMaxLength(100);
        builder.Property(x => x.TargetId).HasMaxLength(200);
        builder.Property(x => x.ActorIdentityIssuer).HasMaxLength(500);
        builder.Property(x => x.ActorIdentitySubject).HasMaxLength(200);
        builder.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAtUtc).HasDefaultValueSql("sysutcdatetime()");
        builder.HasIndex(x => new { x.CreatedAtUtc, x.EventType }).HasDatabaseName("IX_AuditEvents_CreatedAt_EventType");
        builder.HasIndex(x => new { x.ActorAppUserId, x.CreatedAtUtc }).HasDatabaseName("IX_AuditEvents_Actor_CreatedAt");
        builder.HasIndex(x => new { x.TenantId, x.CreatedAtUtc }).HasDatabaseName("IX_AuditEvents_Tenant_CreatedAt");
        builder.HasOne(x => x.ActorAppUser).WithMany(x => x.AuditEvents).HasForeignKey(x => x.ActorAppUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Show).WithMany().HasForeignKey(x => x.ShowId).OnDelete(DeleteBehavior.NoAction);
    }
}
