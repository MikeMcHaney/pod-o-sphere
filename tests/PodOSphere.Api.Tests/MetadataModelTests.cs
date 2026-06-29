using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PodOSphere.Api.Data;

namespace PodOSphere.Api.Tests;

public sealed class MetadataModelTests
{
    private static readonly string[] ExpectedTables =
    [
        "AppUsers",
        "AuditEvents",
        "BillingAccounts",
        "DataSources",
        "Invitations",
        "PlatformRoles",
        "PlatformUserRoles",
        "PortalDomains",
        "PortalSettings",
        "ProcessingJobs",
        "Roles",
        "ShowClaims",
        "Shows",
        "Tenants",
        "TenantUsers",
        "UsageCounters"
    ];

    [Fact]
    public void Model_maps_every_authoritative_mssql_table()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var tables = model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Order()
            .ToArray();

        Assert.Equal(ExpectedTables, tables);
    }

    [Fact]
    public void Model_preserves_critical_defaults_indexes_and_delete_behavior()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        var portalSettings = model.FindEntityType(typeof(PortalSettings))!;
        Assert.Equal("char(7)", portalSettings.FindProperty(nameof(PortalSettings.PrimaryHex))!.GetColumnType());
        Assert.Equal("#2563EB", portalSettings.FindProperty(nameof(PortalSettings.PrimaryHex))!.GetDefaultValue());

        var processingJob = model.FindEntityType(typeof(ProcessingJob))!;
        var priorityIndex = processingJob.GetIndexes()
            .Single(index => index.GetDatabaseName() == "IX_ProcessingJobs_Status_Priority");
        Assert.Equal([false, true, false], priorityIndex.IsDescending);
        Assert.Equal("nvarchar(max)", processingJob.FindProperty(nameof(ProcessingJob.ResultJson))!.GetColumnType());
        Assert.Contains(
            processingJob.GetIndexes(),
            index => index.GetDatabaseName() == "IX_ProcessingJobs_LeaseExpiry" && index.GetFilter() == "[Status] = 'InProgress'");

        var usageCounter = model.FindEntityType(typeof(UsageCounter))!;
        Assert.Contains(
            usageCounter.GetIndexes(),
            index => index.IsUnique && index.GetDatabaseName() == "UQ_UsageCounters_Tenant_Show_Month");

        var dataSource = model.FindEntityType(typeof(DataSource))!;
        Assert.Equal("Full", dataSource.FindProperty(nameof(DataSource.InventoryMode))!.GetDefaultValue());
        Assert.Equal(50, dataSource.FindProperty(nameof(DataSource.InventoryMode))!.GetMaxLength());
        Assert.Contains(
            dataSource.GetIndexes(),
            index => index.IsUnique && index.GetDatabaseName() == "UQ_DataSources_Show_Type_Url");

        var appUser = model.FindEntityType(typeof(AppUser))!;
        var identityIndex = Assert.Single(
            appUser.GetIndexes(),
            index => index.GetDatabaseName() == "UQ_AppUsers_Identity_Issuer_Subject");
        Assert.True(identityIndex.IsUnique);
        Assert.Equal(
            [nameof(AppUser.IdentityIssuer), nameof(AppUser.IdentitySubject)],
            identityIndex.Properties.Select(property => property.Name));
        Assert.Equal(320, appUser.FindProperty(nameof(AppUser.ContactEmail))!.GetMaxLength());
        Assert.False(appUser.FindProperty(nameof(AppUser.ContactEmail))!.IsNullable);
        Assert.Equal(320, appUser.FindProperty(nameof(AppUser.PreferredUsername))!.GetMaxLength());

        var platformUserRole = model.FindEntityType(typeof(PlatformUserRole))!;
        var platformRoleIndex = Assert.Single(
            platformUserRole.GetIndexes(),
            index => index.GetDatabaseName() == "UQ_PlatformUserRoles_User_Role");
        Assert.True(platformRoleIndex.IsUnique);

        var invitation = model.FindEntityType(typeof(Invitation))!;
        Assert.Contains(
            invitation.GetIndexes(),
            index => index.IsUnique && index.Properties.Single().Name == nameof(Invitation.InvitationTokenHash));

        var auditEvent = model.FindEntityType(typeof(AuditEvent))!;
        Assert.Equal("nvarchar(max)", auditEvent.FindProperty(nameof(AuditEvent.MetadataJson))!.GetColumnType());
        Assert.Contains(
            auditEvent.GetIndexes(),
            index => index.GetDatabaseName() == "IX_AuditEvents_CreatedAt_EventType");

        var showClaim = model.FindEntityType(typeof(ShowClaim))!;
        Assert.Equal("Pending", showClaim.FindProperty(nameof(ShowClaim.Status))!.GetDefaultValue());
        Assert.Contains(
            showClaim.GetIndexes(),
            index => index.GetDatabaseName() == "IX_ShowClaims_RequestingUser_Status");

        var foreignKeys = model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys());
        Assert.All(foreignKeys, foreignKey => Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior));
    }

    private static MetadataDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MetadataDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelTests;User Id=sa;Password=NotUsed_123!;TrustServerCertificate=True")
            .Options;

        return new MetadataDbContext(options);
    }
}
