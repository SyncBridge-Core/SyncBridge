using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using Xunit.Abstractions;

namespace SyncBridge.Core.Tests;

[Collection(SqlServerRecoveryCollection.Name)]
[Trait("Category", "LiveSqlServer")]
public sealed class SqlServerBatchBIntegrationTests(ITestOutputHelper output)
{
    private const string Quality = "demo.quality";
    private const string Actor = SecuredServiceFixture.Operator;

    [ApprovedSqlServerFact]
    public async Task CompleteManufacturingPath_RoundTripsSecurityOptionalFieldsAndEvidenceOnSqlServer()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        var order = NewOrder(); var batch = order + "B"; var deviation = order + "D";
        try
        {
            await using (var auth = f.Scope())
            {
                var credentials = auth.ServiceProvider.GetRequiredService<ICredentialAuthenticator>();
                Assert.Equal(Actor, (await credentials.AuthenticateAsync(Actor, SecuredServiceFixture.Password))!.UserId);
                Assert.Null(await credentials.AuthenticateAsync(Actor, "wrong-password"));
            }
            await Prepare(f, order, batch);
            Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(Actor, s => s.StartProcessingAsync(batch))).Outcome);
            var review = Assert.Single((await f.Batch(Quality, s => s.ReviewExceptionsAsync())).Value!, r => r.BatchNumber == batch);
            Assert.Single(review.MissingOrFailedMaterials); Assert.Single(review.MissingOrFailedEquipment);
            Assert.True((await f.Batch(Actor, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Failed))).Succeeded);
            Assert.True((await f.Batch(Actor, s => s.VerifyEquipmentAsync(batch, "EQ-DEMO-001", VerificationResult.Failed))).Succeeded);
            Guid materialIdentity, equipmentIdentity;
            await using (var fields = f.Scope())
            {
                var db = fields.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
                var id = await db.Batches.Where(r => r.BatchNumber == batch).Select(r => r.BatchId).SingleAsync();
                var mv = await db.MaterialVerifications.SingleAsync(r => r.BatchId == id); materialIdentity = mv.VerificationId;
                mv.LotNumber = "S4B-FICTIONAL-LOT"; mv.Comments = "Retain fictional material comment";
                var ev = await db.EquipmentVerifications.SingleAsync(r => r.BatchId == id); equipmentIdentity = ev.VerificationId;
                ev.CalibrationValid = true; ev.Comments = "Retain fictional calibration comment";
                (await db.Batches.SingleAsync(r => r.BatchId == id)).FinalDisposition = "Retain fictional disposition";
                await db.SaveChangesAsync();
            }
            await VerifyAndStart(f, batch);
            Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(Actor, s => s.StartStepAsync(batch, 2))).Outcome);
            var first = (await f.Batch(Actor, s => s.StartStepAsync(batch, 1))).Value!.Steps.Single().ExecutionId;
            Assert.True((await f.Batch(Actor, s => s.CompleteStepAsync(batch, first, recordedData: "Fictional process 20.125"))).Succeeded);
            var second = (await f.Batch(Actor, s => s.StartStepAsync(batch, 2))).Value!.Steps.Single(r => r.StepNumber == 2).ExecutionId;
            Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(Actor, s => s.CompleteStepAsync(batch, second))).Outcome);
            Assert.Equal(OperationOutcome.NotFound, (await f.Batch(Quality, s => s.SignStepAsync(batch, Guid.NewGuid().ToString("D"), ElectronicSignatureMeaning.Approval))).Outcome);
            Assert.True((await f.Batch(Actor, s => s.CreateDeviationAsync(batch, deviation, "Fictional controlled interruption", second))).Succeeded);
            Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(Actor, s => s.ResumeAsync(batch))).Outcome);
            Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(Actor, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.Approve))).Outcome);
            Assert.True((await f.Batch(Quality, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.BeginReview, "Fictional quality review"))).Succeeded);
            Assert.True((await f.Batch(Quality, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.Approve))).Succeeded);
            Assert.True((await f.Batch(Quality, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.Resolve, "Fictional acceptable disposition"))).Succeeded);
            var resolved = (await f.Batch(Actor, s => s.GetAsync(batch))).Value!;
            Assert.Equal(BatchStatus.Exception, resolved.Status); Assert.Equal(DeviationStatus.Closed, resolved.Deviations.Single().Status);
            Assert.True((await f.Batch(Actor, s => s.ResumeAsync(batch))).Succeeded);
            var signature = (await f.Batch(Quality, s => s.SignStepAsync(batch, second, ElectronicSignatureMeaning.Approval))).Value!.Signatures.Single();
            Assert.Equal(Quality, signature.ActorId);
            Assert.True((await f.Batch(Actor, s => s.CompleteStepAsync(batch, second, signature.SignatureId))).Succeeded);
            Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(Quality, s => s.CompleteAsync(batch))).Outcome);
            Assert.True((await f.Batch(Actor, s => s.CompleteAsync(batch))).Succeeded); // Actor explicitly has Supervisor too.
            var readTimer = System.Diagnostics.Stopwatch.StartNew();
            var completed = (await f.Batch(Quality, s => s.GetAsync(batch))).Value!;
            readTimer.Stop();
            Assert.True(readTimer.Elapsed < TimeSpan.FromSeconds(2), "A normal demonstration batch read should meet the approximate two-second interaction target.");
            Assert.Equal(BatchStatus.Completed, completed.Status); Assert.Equal(100.125m, completed.PlannedQuantity);
            Assert.Equal("Fictional process 20.125", completed.Steps.Single(r => r.StepNumber == 1).RecordedData);
            Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(Actor, s => s.CreateDeviationAsync(batch, order + "X", "Forbidden completed change"))).Outcome);
            Assert.DoesNotContain((await f.Batch(Quality, s => s.ReviewExceptionsAsync())).Value!, r => r.BatchNumber == batch);
            var audit = (await f.Batch(Quality, s => s.GetAuditAsync(batch))).Value!;
            Assert.Equal(audit.Count, audit.Select(r => r.Id).Distinct().Count());
            Assert.Equal(Quality, audit.Single(r => r.Action == AuditEventType.DeviationResolved).ActorId);
            Assert.Equal(Actor, audit.Single(r => r.Action == AuditEventType.BatchCompleted).ActorId);
            Assert.Equal(2, audit.Count(r => r.Action == AuditEventType.MaterialVerified));
            Assert.Equal(2, audit.Count(r => r.Action == AuditEventType.EquipmentVerified));
            await using var final = f.Scope();
            var context = final.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            var batchId = await context.Batches.Where(r => r.BatchNumber == batch).Select(r => r.BatchId).SingleAsync();
            var material = await context.MaterialVerifications.SingleAsync(r => r.BatchId == batchId);
            Assert.Equal(materialIdentity, material.VerificationId); Assert.Equal("S4B-FICTIONAL-LOT", material.LotNumber); Assert.Equal("Retain fictional material comment", material.Comments);
            var equipment = await context.EquipmentVerifications.SingleAsync(r => r.BatchId == batchId);
            Assert.Equal(equipmentIdentity, equipment.VerificationId); Assert.True(equipment.CalibrationValid); Assert.Equal("Retain fictional calibration comment", equipment.Comments);
            Assert.Equal("Retain fictional disposition", (await context.Batches.SingleAsync(r => r.BatchId == batchId)).FinalDisposition);
            var orderAudit = await context.AuditEvents.Where(r => r.BatchId == null && r.AffectedEntity == "ProductionOrder" && r.AffectedEntityId == order).ToArrayAsync();
            Assert.Equal(2, orderAudit.Length); Assert.All(orderAudit, r => Assert.Equal(Actor, r.UserId));
            output.WriteLine($"SQL end-to-end: Completed; {audit.Count} Batch audits; {orderAudit.Length} standalone order audits; optional fields/identities preserved; fresh-scope resolved Exception resumed.");
            output.WriteLine($"Fresh-scope secured Batch read: {readTimer.Elapsed.TotalMilliseconds:F3} ms on this demonstration fixture; no production-scale qualification.");
        }
        finally { await Cleanup(f, order); }
    }

    [ApprovedSqlServerFact]
    public async Task SqlConstraintFailure_IsSanitizedRolledBackAndServiceScopeCanBeReused()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        var order = NewOrder();
        try
        {
            await using var scope = f.Scope(Actor);
            var service = scope.ServiceProvider.GetRequiredService<ProductionOrderService>();
            f.FailNextOrderWithSqlConstraint();
            var failed = await service.CreateAsync(Request(order));
            Assert.Equal(OperationOutcome.TechnicalFailure, failed.Outcome);
            Assert.DoesNotContain("ProductName", failed.Message!);
            Assert.Contains(f.Errors, e => SqlNumbers(e).Contains(515));
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            Assert.Empty(db.ChangeTracker.Entries());
            Assert.False(await db.ProductionOrders.AnyAsync(r => r.OrderNumber == order));
            Assert.False(await db.AuditEvents.AnyAsync(r => r.AffectedEntityId == order));
            Assert.True((await service.CreateAsync(Request(order))).Succeeded);
            Assert.Single(await db.AuditEvents.Where(r => r.AffectedEntityId == order).ToArrayAsync());
            output.WriteLine("SQL error 515: sanitized TechnicalFailure; order and audit absent after rollback; same service scope reused successfully.");
        }
        finally { await Cleanup(f, order); }
    }

    [ApprovedSqlServerFact]
    public async Task SqlVerificationAndSignatureFailures_RollBackBothStateAndEvidence()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        var order = NewOrder(); var batch = order + "B";
        try
        {
            await Prepare(f, order, batch);
            Assert.True((await f.Batch(Actor, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Failed))).Succeeded);
            var before = (await f.Batch(Quality, s => s.GetAuditAsync(batch))).Value!.Count;
            f.FailAfterFirstWrite();
            Assert.Equal(OperationOutcome.TechnicalFailure, (await f.Batch(Actor, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Passed))).Outcome);
            Assert.Equal(VerificationResult.Failed, (await f.Batch(Actor, s => s.GetAsync(batch))).Value!.Materials.Single().Result);
            Assert.Equal(before, (await f.Batch(Quality, s => s.GetAuditAsync(batch))).Value!.Count);
            await VerifyAndStart(f, batch);
            var execution = (await f.Batch(Actor, s => s.StartStepAsync(batch, 1))).Value!.Steps.Single().ExecutionId;
            before = (await f.Batch(Quality, s => s.GetAuditAsync(batch))).Value!.Count;
            f.FailAfterFirstWrite();
            Assert.Equal(OperationOutcome.TechnicalFailure, (await f.Batch(Quality, s => s.SignStepAsync(batch, execution, ElectronicSignatureMeaning.Approval))).Outcome);
            Assert.Empty((await f.Batch(Actor, s => s.GetAsync(batch))).Value!.Signatures);
            Assert.Equal(before, (await f.Batch(Quality, s => s.GetAuditAsync(batch))).Value!.Count);
            output.WriteLine("SQL executed-write failures: current verification plus audit rolled back; signature plus audit rolled back; subsequent commands succeeded.");
        }
        finally { await Cleanup(f, order); }
    }

    [ApprovedSqlServerFact]
    public async Task CorruptPersistedDomainData_IsReportedAsTechnicalFailure()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        var order = NewOrder(); var batch = order + "B";
        var recipeId = Guid.NewGuid(); var recipeCode = order + "R";
        try
        {
            await Prepare(f, order, batch);
            await using (var corruption = f.Scope())
            {
                var db = corruption.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
                (await db.Batches.SingleAsync(r => r.BatchNumber == batch)).Status = BatchStatus.Exception;
                await db.SaveChangesAsync(); // Only this test-owned fixture is deliberately corrupted.
            }
            var batchResult = await f.Batch(Quality, s => s.GetAsync(batch));
            await using (var corruption = f.Scope())
            {
                var db = corruption.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
                (await db.ProductionOrders.SingleAsync(r => r.OrderNumber == order)).Quantity = 0;
                db.Recipes.Add(new RecipeRow { RecipeId = recipeId, RecipeCode = recipeCode, Name = "", Version = "1.0", IsApproved = true });
                await db.SaveChangesAsync();
            }
            var orderResult = await f.Order(Actor, s => s.GetAsync(order));
            var recipeResult = await f.Order(Actor, s => s.CreateAsync(Request(order) with { RecipeCode = recipeCode }));
            Assert.Equal((OperationOutcome.TechnicalFailure, OperationOutcome.TechnicalFailure, OperationOutcome.TechnicalFailure), (batchResult.Outcome, orderResult.Outcome, recipeResult.Outcome));
            Assert.NotEmpty(f.Errors);
            output.WriteLine("Corrupt persisted Batch/order/recipe data: TechnicalFailure with diagnostic logging, rather than an input/business rejection.");
        }
        finally
        {
            await Cleanup(f, order);
            await using var cleanup = f.Scope();
            var db = cleanup.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            await db.Recipes.Where(r => r.RecipeId == recipeId && r.RecipeCode == recipeCode).ExecuteDeleteAsync();
        }
    }

    [ApprovedSqlServerFact]
    public async Task ConcurrentCommands_OnSameBatchPermitOneTransitionWithoutDuplicateEvidence()
    {
        await using var f = SecuredServiceFixture.CreateSqlServer();
        await using var other = SecuredServiceFixture.CreateSqlServer();
        var order = NewOrder(); var batch = order + "B";
        try
        {
            Assert.True((await f.Order(Actor, s => s.CreateAsync(Request(order)))).Succeeded);
            Assert.True((await f.Order(Actor, s => s.ChangeStatusAsync(order, ProductionOrderAction.Release))).Succeeded);
            Assert.True((await f.Batch(Actor, s => s.InitiateAsync(order, batch))).Succeeded);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var attempts = new[] { f, other }.Select(instance => Task.Run(async () =>
            {
                await gate.Task;
                return await instance.Batch(Actor, s => s.MarkReadyAsync(batch, deadline.Token));
            })).ToArray();
            gate.SetResult();
            var results = await Task.WhenAll(attempts);
            Assert.Equal(1, results.Count(r => r.Succeeded));
            Assert.All(results.Where(r => !r.Succeeded), r => Assert.Contains(r.Outcome, new[] { OperationOutcome.BusinessRuleRejected, OperationOutcome.TechnicalFailure }));
            var errors = f.Errors.Concat(other.Errors).SelectMany(SqlNumbers).Distinct().ToArray();
            if (results.Any(r => r.Outcome == OperationOutcome.TechnicalFailure)) Assert.Contains(1205, errors);
            Assert.DoesNotContain(-2, errors); // A timeout is not an accepted deadlock-victim outcome.
            Assert.Equal(BatchStatus.Ready, (await f.Batch(Actor, s => s.GetAsync(batch))).Value!.Status);
            var audits = (await f.Batch(Quality, s => s.GetAuditAsync(batch))).Value!;
            Assert.Single(audits, r => r.Action == AuditEventType.BatchMarkedReady);
            Assert.Equal(2, audits.Count);
            output.WriteLine($"Concurrent Ready commands: {string.Join(", ", results.Select(r => r.Outcome))}; SQL error codes: {string.Join(",", errors)}; one transition and one transition audit.");
        }
        finally { await Cleanup(f, order); }
    }

    private static string NewOrder() => "S4B-" + Guid.NewGuid().ToString("N");
    private static CreateProductionOrderRequest Request(string number) => new(number, "S4B-FICTIONAL", "Fictional SQL verification product", "RCP-DEMO-001", 100.125m);
    private static async Task Prepare(SecuredServiceFixture f, string order, string batch)
    {
        Assert.True((await f.Order(Actor, s => s.CreateAsync(Request(order)))).Succeeded);
        Assert.True((await f.Order(Actor, s => s.ChangeStatusAsync(order, ProductionOrderAction.Release))).Succeeded);
        Assert.True((await f.Batch(Actor, s => s.InitiateAsync(order, batch))).Succeeded);
        Assert.True((await f.Batch(Actor, s => s.MarkReadyAsync(batch))).Succeeded);
    }
    private static async Task VerifyAndStart(SecuredServiceFixture f, string batch)
    {
        Assert.True((await f.Batch(Actor, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Passed))).Succeeded);
        Assert.True((await f.Batch(Actor, s => s.VerifyEquipmentAsync(batch, "EQ-DEMO-001", VerificationResult.Passed))).Succeeded);
        Assert.True((await f.Batch(Actor, s => s.StartProcessingAsync(batch))).Succeeded);
    }
    private static IEnumerable<int> SqlNumbers(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is SqlException sql) foreach (SqlError error in sql.Errors) yield return error.Number;
    }
    internal static async Task Cleanup(SecuredServiceFixture fixture, string order)
    {
        if (!order.StartsWith("S4B-", StringComparison.Ordinal) || !Guid.TryParseExact(order[4..], "N", out _)) throw new InvalidOperationException("Cleanup requires an exact test-owned order number.");
        await using var scope = fixture.Scope();
        var context = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("""
            DECLARE @OrderId uniqueidentifier = (SELECT OrderId FROM dbo.ProductionOrder WHERE OrderNumber = @number);
            DECLARE @batches TABLE (BatchId uniqueidentifier PRIMARY KEY);
            INSERT INTO @batches SELECT BatchId FROM dbo.Batch WHERE OrderId = @OrderId;
            DELETE FROM dbo.ElectronicSignature WHERE BatchId IN (SELECT BatchId FROM @batches);
            DELETE FROM dbo.MaterialVerification WHERE BatchId IN (SELECT BatchId FROM @batches);
            DELETE FROM dbo.EquipmentVerification WHERE BatchId IN (SELECT BatchId FROM @batches);
            DELETE FROM dbo.Deviation WHERE BatchId IN (SELECT BatchId FROM @batches);
            DELETE FROM dbo.BatchStepExecution WHERE BatchId IN (SELECT BatchId FROM @batches);
            DELETE FROM dbo.AuditEvent WHERE BatchId IN (SELECT BatchId FROM @batches)
                OR (BatchId IS NULL AND AffectedEntity = N'ProductionOrder' AND AffectedEntityId = @number);
            DELETE FROM dbo.Batch WHERE OrderId = @OrderId;
            DELETE FROM dbo.ProductionOrder WHERE OrderId = @OrderId AND OrderNumber = @number;
            """, new SqlParameter("@number", order));
        await transaction.CommitAsync();
        Assert.False(await context.ProductionOrders.AnyAsync(r => r.OrderNumber == order));
    }
}
