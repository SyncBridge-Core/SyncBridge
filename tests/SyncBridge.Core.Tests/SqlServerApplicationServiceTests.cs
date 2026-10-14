using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Tests;

/// <summary>Explicit opt-in is in addition to the required Team approval for unsandboxed live SQL execution.</summary>
public sealed class ApprovedSqlServerFactAttribute : FactAttribute
{
    public ApprovedSqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SYNCBRIDGE_APPROVED_LIVE_SQL") != "1")
            Skip = "Requires Team-approved live SQL execution and SYNCBRIDGE_APPROVED_LIVE_SQL=1.";
    }
}

[Collection(SqlServerRecoveryCollection.Name)]
[Trait("Category", "LiveSqlServer")]
public sealed class SqlServerApplicationServiceTests
{
    [ApprovedSqlServerFact]
    public async Task TrustedActorAndManufacturingRead_UseAcceptedSqlServerFoundation()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        var result = await f.Order(SecuredServiceFixture.Operator, s => s.GetAsync("PO-DEMO-001"));
        Assert.True(result.Succeeded);
        Assert.Equal("PO-DEMO-001", result.Value!.OrderNumber);
    }

    [ApprovedSqlServerFact]
    public async Task InducedServiceFailure_RollsBackOrderAndAuditOnSqlServer()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        var number = "S4-ROLLBACK-" + Guid.NewGuid().ToString("N")[..16];
        f.FailAfterFirstWrite();
        var result = await f.Order(SecuredServiceFixture.Operator, s => s.CreateAsync(new(number, "TEST-P", "Fictional rollback fixture", "RCP-DEMO-001", 1.125m)));
        Assert.Equal(OperationOutcome.TechnicalFailure, result.Outcome);
        await using var scope = f.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.False(await db.ProductionOrders.AnyAsync(row => row.OrderNumber == number));
        Assert.False(await db.AuditEvents.AnyAsync(row => row.AffectedEntityId == number));
    }
}
