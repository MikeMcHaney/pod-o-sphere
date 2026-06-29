using Microsoft.EntityFrameworkCore;

namespace PodOSphere.Api.Data;

public sealed class MetadataDbContext(DbContextOptions<MetadataDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<PlatformUserRole> PlatformUserRoles => Set<PlatformUserRole>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    public DbSet<Show> Shows => Set<Show>();
    public DbSet<PortalSettings> PortalSettings => Set<PortalSettings>();
    public DbSet<PortalDomain> PortalDomains => Set<PortalDomain>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<BillingAccount> BillingAccounts => Set<BillingAccount>();
    public DbSet<UsageCounter> UsageCounters => Set<UsageCounter>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ShowClaim> ShowClaims => Set<ShowClaim>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MetadataDbContext).Assembly);
    }
}
