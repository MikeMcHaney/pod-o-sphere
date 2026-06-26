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
        "BillingAccounts",
        "DataSources",
        "PortalDomains",
        "PortalSettings",
        "ProcessingJobs",
        "Roles",
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

        var usageCounter = model.FindEntityType(typeof(UsageCounter))!;
        Assert.Contains(
            usageCounter.GetIndexes(),
            index => index.IsUnique && index.GetDatabaseName() == "UQ_UsageCounters_Tenant_Show_Month");

        var appUser = model.FindEntityType(typeof(AppUser))!;
        var identityIndex = Assert.Single(
            appUser.GetIndexes(),
            index => index.GetDatabaseName() == "UQ_AppUsers_Identity_Issuer_Subject");
        Assert.True(identityIndex.IsUnique);
        Assert.Equal(
            [nameof(AppUser.IdentityIssuer), nameof(AppUser.IdentitySubject)],
            identityIndex.Properties.Select(property => property.Name));

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
