using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;

namespace SyncBridge.Core.Tests;

public sealed class SecuredApplicationServiceTests
{
    [Fact]
    public async Task EndToEndManufacturing_UsesTrustedActorsAndSurvivesFreshScopes()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        var instructions = (await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"))).Value!.Recipe;
        Assert.Equal(new[] { 1, 2 }, instructions.Steps.Select(s => s.StepNumber));
        Assert.All(instructions.Steps, s => Assert.False(string.IsNullOrWhiteSpace(s.Instructions)));
        Assert.Single(instructions.Materials);
        Assert.Single(instructions.Equipment);
        var started = await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1));
        var first = Assert.Single(started.Value!.Steps).ExecutionId;
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", first, recordedData: "Fictional process value: 20"))).Succeeded);
        var secondStarted = await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 2));
        var second = secondStarted.Value!.Steps.Single(s => s.StepNumber == 2).ExecutionId;
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", second))).Outcome);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CreateDeviationAsync("B-TEST", "DEV-TEST", "Fictional interruption", second))).Succeeded);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.ResumeAsync("B-TEST"))).Outcome);
        var exceptions = await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewExceptionsAsync());
        Assert.Equal("DEV-TEST", Assert.Single(exceptions.Value!).UnresolvedDeviations.Single());
        Assert.True((await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "DEV-TEST", DeviationAction.BeginReview, "Fictional review"))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "DEV-TEST", DeviationAction.Approve))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "DEV-TEST", DeviationAction.Resolve, "Fictional disposition"))).Succeeded);
        // Resolution and resumption are separate commands/roles. This intermediate persisted state must be readable.
        var resolved = await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"));
        Assert.Equal(BatchStatus.Exception, resolved.Value!.Status);
        Assert.Equal(DeviationStatus.Closed, Assert.Single(resolved.Value.Deviations).Status);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.ResumeAsync("B-TEST"))).Succeeded);
        var signed = await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", second, ElectronicSignatureMeaning.Approval));
        var signature = Assert.Single(signed.Value!.Signatures);
        Assert.Equal(SecuredServiceFixture.Quality, signature.ActorId);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", second, signature.SignatureId))).Succeeded);
        var completed = await f.Batch(SecuredServiceFixture.Supervisor, s => s.CompleteAsync("B-TEST"));
        Assert.True(completed.Succeeded);
        Assert.Equal(BatchStatus.Completed, completed.Value!.Status);
        Assert.Equal("Fictional process value: 20", completed.Value.Steps.Single(s => s.StepNumber == 1).RecordedData);
        Assert.Equal(100.125m, completed.Value.PlannedQuantity);
        Assert.Empty((await f.Batch(SecuredServiceFixture.Supervisor, s => s.ReviewExceptionsAsync())).Value!);
        var audits = (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!;
        Assert.Equal(audits.Count, audits.Select(a => a.Id).Distinct().Count());
        Assert.Equal(SecuredServiceFixture.Operator, audits.Single(a => a.Action == AuditEventType.BatchCreated).ActorId);
        Assert.Equal(SecuredServiceFixture.Supervisor, audits.Single(a => a.Action == AuditEventType.BatchCompleted).ActorId);
        Assert.Equal(SecuredServiceFixture.Quality, audits.Single(a => a.Action == AuditEventType.DeviationResolved).ActorId);
        Assert.Contains(audits, a => a.Action == AuditEventType.MaterialVerified);
        Assert.Contains(audits, a => a.Action == AuditEventType.EquipmentVerified);
        Assert.Equal(2, audits.Count(a => a.Action == AuditEventType.StepStarted));
        Assert.Equal(2, audits.Count(a => a.Action == AuditEventType.StepCompleted));
        Assert.Equal(1, audits.Count(a => a.Action == AuditEventType.ElectronicSignatureApplied));
        await using var storedScope = f.Scope();
        var db = storedScope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.Equal("Fictional process value: 20", (await db.BatchStepExecutions.SingleAsync(row => row.StepNumber == 1)).RecordedData);
    }

    [Theory]
    [InlineData(null, OperationOutcome.Unauthenticated)]
    [InlineData("unknown.actor", OperationOutcome.Unauthenticated)]
    [InlineData(SecuredServiceFixture.Operator, OperationOutcome.Forbidden)]
    [InlineData(SecuredServiceFixture.Quality, OperationOutcome.Forbidden)]
    [InlineData(SecuredServiceFixture.Administrator, OperationOutcome.Forbidden)]
    public async Task OrderCreation_DeniesInvalidActorsBeforeMutation(string? actor, OperationOutcome expected)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        Assert.Equal(expected, (await f.Order(actor, s => s.CreateAsync(new("DENIED", "P", "Fictional", "RCP-DEMO-001", 1)))).Outcome);
        await using var scope = f.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.False(await db.ProductionOrders.AnyAsync(p => p.OrderNumber == "DENIED"));
        Assert.Empty(await db.AuditEvents.ToArrayAsync());
    }

    [Theory]
    [InlineData(ProductionOrderAction.Release)]
    [InlineData(ProductionOrderAction.StartProcessing)]
    [InlineData(ProductionOrderAction.Complete)]
    [InlineData(ProductionOrderAction.Cancel)]
    public async Task OrderStatusTransitions_AreSupervisorOnly(ProductionOrderAction action)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        foreach (var actor in new[] { SecuredServiceFixture.Operator, SecuredServiceFixture.Quality, SecuredServiceFixture.Administrator })
            Assert.Equal(OperationOutcome.Forbidden, (await f.Order(actor, s => s.ChangeStatusAsync("PO-DEMO-001", action))).Outcome);
    }

    [Theory]
    [InlineData(DeviationAction.BeginReview)]
    [InlineData(DeviationAction.Approve)]
    [InlineData(DeviationAction.Reject)]
    [InlineData(DeviationAction.Resolve)]
    public async Task DeviationDisposition_IsQualityOnly(DeviationAction action)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        foreach (var actor in new[] { SecuredServiceFixture.Operator, SecuredServiceFixture.Supervisor, SecuredServiceFixture.Administrator })
            Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(actor, s => s.ReviewDeviationAsync("B-TEST", "D", action, "Fictional"))).Outcome);
    }

    [Fact]
    public async Task RoleClaimsAndDuplicateIdentifiers_CannotSpoofTrustedPermissions()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var scope = f.Scope(SecuredServiceFixture.Operator, new Claim(ClaimTypes.Role, ManufacturingRoles.Supervisor));
        var orders = scope.ServiceProvider.GetRequiredService<ProductionOrderService>();
        Assert.Equal(OperationOutcome.Forbidden, (await orders.CreateAsync(new("SPOOF", "P", "Fictional", "RCP-DEMO-001", 1))).Outcome);
        await using var duplicate = f.Scope(SecuredServiceFixture.Supervisor, new Claim(ClaimTypes.NameIdentifier, SecuredServiceFixture.Operator));
        Assert.Equal(OperationOutcome.Unauthenticated, (await duplicate.ServiceProvider.GetRequiredService<ProductionOrderService>().GetAsync("PO-DEMO-001")).Outcome);
        var requestTypes = new[] { typeof(CreateProductionOrderRequest) };
        Assert.All(requestTypes, type => Assert.DoesNotContain(type.GetProperties(), p => p.Name.Contains("Actor") || p.Name.Contains("UserId") || p.Name.Contains("OperatorId")));
    }

    [Fact]
    public async Task MissingAndInactiveManufacturingUser_CannotAuthenticateOrExecute()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using (var scope = f.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.AppUsers.SingleAsync(u => u.UserId == SecuredServiceFixture.Supervisor)).IsActive = false;
            var quality = await db.AppUsers.SingleAsync(u => u.UserId == SecuredServiceFixture.Quality);
            db.AppUsers.Remove(quality);
            await db.SaveChangesAsync();
        }
        foreach (var actor in new[] { SecuredServiceFixture.Supervisor, SecuredServiceFixture.Quality })
        {
            Assert.Equal(OperationOutcome.Unauthenticated, (await f.Order(actor, s => s.GetAsync("PO-DEMO-001"))).Outcome);
            await using var scope = f.Scope();
            Assert.Null(await scope.ServiceProvider.GetRequiredService<ICredentialAuthenticator>().AuthenticateAsync(actor, SecuredServiceFixture.Password));
        }
    }

    [Fact]
    public async Task CredentialAuthentication_RejectsBadPasswordsAndDoesNotTakeAnActorId()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var scope = f.Scope();
        var credentials = scope.ServiceProvider.GetRequiredService<ICredentialAuthenticator>();
        var valid = await credentials.AuthenticateAsync(SecuredServiceFixture.Operator, SecuredServiceFixture.Password);
        Assert.Equal(SecuredServiceFixture.Operator, valid!.UserId);
        Assert.Null(await credentials.AuthenticateAsync(SecuredServiceFixture.Operator, "wrong-password"));
        Assert.Null(await credentials.AuthenticateAsync("unknown", SecuredServiceFixture.Password));
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Operator)]
    [InlineData(SecuredServiceFixture.Supervisor)]
    [InlineData(SecuredServiceFixture.Quality)]
    public async Task ManufacturingReads_AreAvailableToAssignedManufacturingRoles(string actor)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.PrepareAsync();
        Assert.True((await f.Order(actor, s => s.GetAsync("PO-TEST"))).Succeeded);
        Assert.True((await f.Batch(actor, s => s.GetAsync("B-TEST"))).Succeeded);
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Operator)]
    [InlineData(SecuredServiceFixture.Administrator)]
    public async Task AuditAndExceptionReview_DenyOtherRoles(string actor)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(actor, s => s.GetAuditAsync("B-TEST"))).Outcome);
        Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(actor, s => s.ReviewExceptionsAsync())).Outcome);
        Assert.Equal(OperationOutcome.Forbidden, (await f.Order(actor, s => s.GetAuditAsync("PO-TEST"))).Outcome);
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Supervisor)]
    [InlineData(SecuredServiceFixture.Administrator)]
    public async Task ElectronicSignatures_DenyUnauthorizedRoles(string actor)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(actor, s => s.SignStepAsync("B-TEST", Guid.NewGuid().ToString("D"), ElectronicSignatureMeaning.Approval))).Outcome);
    }

    [Fact]
    public async Task AdditiveRoles_PermitSameActorDeviationResolutionWithoutInventedSelfApprovalRule()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        Assert.True((await f.Batch(SecuredServiceFixture.MultiRole, s => s.CreateDeviationAsync("B-TEST", "DEV-MULTI", "Fictional"))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.MultiRole, s => s.ReviewDeviationAsync("B-TEST", "DEV-MULTI", DeviationAction.BeginReview, "Review"))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.MultiRole, s => s.ReviewDeviationAsync("B-TEST", "DEV-MULTI", DeviationAction.Approve))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.MultiRole, s => s.ReviewDeviationAsync("B-TEST", "DEV-MULTI", DeviationAction.Resolve, "Disposition"))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.MultiRole, s => s.ResumeAsync("B-TEST"))).Succeeded);
    }

    [Fact]
    public async Task ProductionOrderAudit_ExistsWithoutBatchAndCapturesEveryStatusCommand()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        Assert.True((await f.Order(SecuredServiceFixture.Supervisor, s => s.CreateAsync(new("PO-AUDIT", "P", "Fictional", "RCP-DEMO-001", 10.001m)))).Succeeded);
        foreach (var action in new[] { ProductionOrderAction.Release, ProductionOrderAction.StartProcessing, ProductionOrderAction.Complete })
            Assert.True((await f.Order(SecuredServiceFixture.Supervisor, s => s.ChangeStatusAsync("PO-AUDIT", action))).Succeeded);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Order(SecuredServiceFixture.Supervisor, s => s.ChangeStatusAsync("PO-AUDIT", ProductionOrderAction.Cancel))).Outcome);
        var audits = (await f.Order(SecuredServiceFixture.Quality, s => s.GetAuditAsync("PO-AUDIT"))).Value!;
        Assert.Equal(4, audits.Count);
        Assert.Equal(AuditEventType.ProductionOrderCreated, audits[0].Action);
        Assert.All(audits, a => Assert.Equal(SecuredServiceFixture.Supervisor, a.ActorId));
        await using var scope = f.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.Empty(await db.Batches.ToArrayAsync());
        Assert.All(await db.AuditEvents.ToArrayAsync(), row => Assert.Null(row.BatchId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.0001)]
    public async Task InvalidQuantity_ReturnsInvalidInputAndPersistsNothing(decimal quantity)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        Assert.Equal(OperationOutcome.InvalidInput, (await f.Order(SecuredServiceFixture.Supervisor, s => s.CreateAsync(new("PO-BAD", "P", "Fictional", "RCP-DEMO-001", quantity)))).Outcome);
        await using var scope = f.Scope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>().AuditEvents.ToArrayAsync());
    }

    [Fact]
    public async Task InvalidInputMissingResourcesAndUnapprovedRecipe_AreDistinct()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        Assert.Equal(OperationOutcome.InvalidInput, (await f.Order(SecuredServiceFixture.Supervisor, s => s.GetAsync(""))).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await f.Order(SecuredServiceFixture.Supervisor, s => s.GetAsync("MISSING"))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.InitiateAsync("PO-DEMO-001", "B-PLANNED"))).Outcome);
        await using (var scope = f.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.Recipes.SingleAsync()).IsApproved = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Order(SecuredServiceFixture.Supervisor, s => s.CreateAsync(new("PO-NO", "P", "Fictional", "RCP-DEMO-001", 1)))).Outcome);
    }

    [Fact]
    public async Task MissingFailedPrerequisitesAndReviewQueries_RetainReady()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.PrepareAsync();
        var missing = Assert.Single((await f.Batch(SecuredServiceFixture.Supervisor, s => s.ReviewExceptionsAsync())).Value!);
        Assert.Equal("MAT-DEMO-001", missing.MissingOrFailedMaterials.Single());
        Assert.Equal("EQ-DEMO-001", missing.MissingOrFailedEquipment.Single());
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync("B-TEST"))).Outcome);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Failed))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync("B-TEST", "EQ-DEMO-001", VerificationResult.Passed))).Succeeded);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync("B-TEST"))).Outcome);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Passed))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync("B-TEST", "EQ-DEMO-001", VerificationResult.Failed))).Succeeded);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync("B-TEST"))).Outcome);
        Assert.Equal(BatchStatus.Ready, (await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"))).Value!.Status);
        var audits = (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!;
        Assert.Equal(2, audits.Count(a => a.Action == AuditEventType.MaterialVerified));
        Assert.Equal(2, audits.Count(a => a.Action == AuditEventType.EquipmentVerified));
    }

    [Fact]
    public async Task SkippedStepsInvalidVerificationAndForeignSignatureTargets_LeaveNoPartialEvidence()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 2))).Outcome);
        Assert.Empty((await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"))).Value!.Steps);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Passed))).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", Guid.NewGuid().ToString("D"), ElectronicSignatureMeaning.Approval))).Outcome);
        Assert.Equal(OperationOutcome.InvalidInput, (await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", "NOT-A-GUID", ElectronicSignatureMeaning.Approval))).Outcome);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.InitiateAsync("PO-TEST", "B-OTHER"))).Succeeded);
        var started = await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1));
        var target = Assert.Single(started.Value!.Steps).ExecutionId;
        Assert.Equal(OperationOutcome.NotFound, (await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-OTHER", target, ElectronicSignatureMeaning.Approval))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Supervisor, s => s.CompleteAsync("B-TEST"))).Outcome);
    }

    [Fact]
    public async Task OptionalPersistenceFieldsAndVerificationIdentities_SurviveUnrelatedCommands()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.PrepareAsync();
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Failed))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync("B-TEST", "EQ-DEMO-001", VerificationResult.Failed))).Succeeded);
        Guid materialId, equipmentId;
        await using (var scope = f.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            var material = await db.MaterialVerifications.SingleAsync(); materialId = material.VerificationId;
            material.LotNumber = "FICTIONAL-LOT"; material.Comments = "Preserve material comment";
            var equipment = await db.EquipmentVerifications.SingleAsync(); equipmentId = equipment.VerificationId;
            equipment.CalibrationValid = true; equipment.Comments = "Preserve equipment comment";
            (await db.Batches.SingleAsync()).FinalDisposition = "Preserve optional disposition";
            await db.SaveChangesAsync();
        }
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Passed))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync("B-TEST", "EQ-DEMO-001", VerificationResult.Passed))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync("B-TEST"))).Succeeded);
        var start = await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1));
        var executionId = start.Value!.Steps.Single().ExecutionId;
        await using (var scope = f.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.BatchStepExecutions.SingleAsync()).RecordedData = "Preserve fictional recorded value";
            await db.SaveChangesAsync();
        }
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", executionId))).Succeeded);
        await using var verificationScope = f.Scope();
        var stored = verificationScope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var mv = await stored.MaterialVerifications.SingleAsync();
        Assert.Equal(materialId, mv.VerificationId); Assert.Equal("FICTIONAL-LOT", mv.LotNumber); Assert.Equal("Preserve material comment", mv.Comments);
        var ev = await stored.EquipmentVerifications.SingleAsync();
        Assert.Equal(equipmentId, ev.VerificationId); Assert.True(ev.CalibrationValid); Assert.Equal("Preserve equipment comment", ev.Comments);
        Assert.Equal("Preserve optional disposition", (await stored.Batches.SingleAsync()).FinalDisposition);
        Assert.Equal("Preserve fictional recorded value", (await stored.BatchStepExecutions.SingleAsync()).RecordedData);
    }

    [Fact]
    public async Task PersistenceFailure_AfterFirstWriteRollsBackAndSameScopeCannotLeakFailedChanges()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var scope = f.Scope(SecuredServiceFixture.Supervisor);
        var service = scope.ServiceProvider.GetRequiredService<ProductionOrderService>();
        f.FailAfterFirstWrite();
        var result = await service.CreateAsync(new("PO-ROLLBACK", "P", "Fictional", "RCP-DEMO-001", 1));
        Assert.Equal(OperationOutcome.TechnicalFailure, result.Outcome);
        Assert.DoesNotContain("private diagnostic", result.Message!);
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.False(await db.ProductionOrders.AnyAsync(p => p.OrderNumber == "PO-ROLLBACK"));
        Assert.Empty(await db.AuditEvents.ToArrayAsync());
        Assert.True((await service.CreateAsync(new("PO-AFTER", "P", "Fictional", "RCP-DEMO-001", 1))).Succeeded);
        Assert.False(await db.ProductionOrders.AnyAsync(p => p.OrderNumber == "PO-ROLLBACK"));
        Assert.Single(await db.AuditEvents.ToArrayAsync());
    }

    [Fact]
    public async Task AuditMutation_IsRejectedByActualUnitOfWork()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.PrepareAsync();
        await using var scope = f.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        var audit = await db.AuditEvents.FirstAsync();
        audit.Details = "Forbidden edit";
        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>().CommitAsync());
        db.ChangeTracker.Clear();
        db.AuditEvents.Remove(await db.AuditEvents.FirstAsync());
        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>().CommitAsync());
    }
}
