using MedFlow.Application.Interfaces;
using MedFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace MedFlow.UnitTests.Helpers;

/// <summary>
/// Creates isolated in-memory ApplicationDbContext instances for tests.
/// </summary>
internal static class DbContextFactory
{
    /// <summary>Creates a context scoped to a specific tenant with a unique isolated database.</summary>
    public static ApplicationDbContext Create(Guid? tenantId = null)
    {
        var tid = tenantId ?? Guid.NewGuid();
        return CreateOnDatabase(Guid.NewGuid().ToString(), tid);
    }

    /// <summary>
    /// Creates two contexts pointing at the SAME in-memory database but scoped to different tenants.
    /// Used for tenant isolation tests.
    /// </summary>
    public static (ApplicationDbContext CtxA, ApplicationDbContext CtxB) CreateTwentyTenants(
        out Guid tenantAId, out Guid tenantBId)
    {
        var dbName = Guid.NewGuid().ToString();
        tenantAId = Guid.NewGuid();
        tenantBId = Guid.NewGuid();
        return (CreateOnDatabase(dbName, tenantAId), CreateOnDatabase(dbName, tenantBId));
    }

    /// <summary>Creates a context that bypasses all tenant filters (for seeding cross-tenant data).</summary>
    public static ApplicationDbContext CreateWithIgnoredFilter(string? dbName = null)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns((Guid?)null);
        tenant.Setup(t => t.IgnoreTenantFilter).Returns(true);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options, tenant.Object);
    }

    private static ApplicationDbContext CreateOnDatabase(string dbName, Guid tenantId)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.Setup(t => t.TenantId).Returns(tenantId);
        tenant.Setup(t => t.IgnoreTenantFilter).Returns(false);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options, tenant.Object);
    }
}
