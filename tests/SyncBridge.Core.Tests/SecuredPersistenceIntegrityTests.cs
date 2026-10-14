using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;

namespace SyncBridge.Core.Tests;

public sealed class SecuredPersistenceIntegrityTests
{
    [Fact]
    public async Task SignaturesAreImmutableAndCompletedBatchAndChildrenAreProtected()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        var first = (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1))).Value!.Steps.Single().ExecutionId;
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", first))).Succeeded);
        var second = (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 2))).Value!.Steps.Single(s => s.StepNumber == 2).ExecutionId;
        var signature = (await f.Batch(SecuredServiceFixture.Operator, s => s.SignStepAsync("B-TEST", second, ElectronicSignatureMeaning.Completion))).Value!.Signatures.Single().SignatureId;
        await using (var scope = f.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.ElectronicSignatures.SingleAsync()).Meaning = ElectronicSignatureMeaning.Release;
            await Assert.ThrowsAsync<PersistenceIntegrityException>(() => scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>().CommitAsync());
            db.ChangeTracker.Clear();
            db.ElectronicSignatures.Remove(await db.ElectronicSignatures.SingleAsync());
            await Assert.ThrowsAsync<PersistenceIntegrityException>(() => scope.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>().CommitAsync());
        }
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", second, signature))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Supervisor, s => s.CompleteAsync("B-TEST"))).Succeeded);
        var initialAudits = (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!.Count;
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Failed))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.CreateDeviationAsync("B-TEST", "NO", "Fictional"))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", second, ElectronicSignatureMeaning.Approval))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Supervisor, s => s.CompleteAsync("B-TEST"))).Outcome);
        Assert.Equal(initialAudits, (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!.Count);
        await using var direct = f.Scope();
        var context = direct.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        (await context.BatchStepExecutions.FirstAsync()).RecordedData = "Prohibited mutation";
        await Assert.ThrowsAsync<PersistenceIntegrityException>(() => direct.ServiceProvider.GetRequiredService<IApplicationUnitOfWork>().CommitAsync());
    }

    [Fact]
    public async Task VerificationFailure_RollsBackCurrentResultAndAuditTogether()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.PrepareAsync();
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Failed))).Succeeded);
        var before = (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!.Count;
        f.FailAfterFirstWrite();
        Assert.Equal(OperationOutcome.TechnicalFailure, (await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Passed))).Outcome);
        var batch = (await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"))).Value!;
        Assert.Equal(VerificationResult.Failed, batch.Materials.Single().Result);
        Assert.Equal(before, (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!.Count);
    }

    [Fact]
    public async Task SignatureFailure_PersistsNeitherSignatureNorItsAudit()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        var execution = (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1))).Value!.Steps.Single().ExecutionId;
        var before = (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!.Count;
        f.FailAfterFirstWrite();
        Assert.Equal(OperationOutcome.TechnicalFailure, (await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", execution, ElectronicSignatureMeaning.Approval))).Outcome);
        Assert.Empty((await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"))).Value!.Signatures);
        Assert.Equal(before, (await f.Batch(SecuredServiceFixture.Quality, s => s.GetAuditAsync("B-TEST"))).Value!.Count);
    }

    [Fact]
    public async Task RoleRevocation_IsEffectiveWithinAnExistingServiceScope()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var scope = f.Scope(SecuredServiceFixture.Supervisor);
        var service = scope.ServiceProvider.GetRequiredService<ProductionOrderService>();
        Assert.True((await service.GetAsync("PO-DEMO-001")).Succeeded);
        f.Configuration["ManufacturingIdentity:Accounts:1:Roles:0"] = ManufacturingRoles.Administrator;
        ((IConfigurationRoot)f.Configuration).Reload();
        Assert.Equal(OperationOutcome.Forbidden, (await service.ChangeStatusAsync("PO-DEMO-001", ProductionOrderAction.Release)).Outcome);
    }

    [Fact]
    public async Task WrongSignatureAndRejectedDeviation_CannotAdvanceWorkflow()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        var first = (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 1))).Value!.Steps.Single().ExecutionId;
        var wrongSignature = (await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", first, ElectronicSignatureMeaning.Approval))).Value!.Signatures.Single().SignatureId;
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", first))).Succeeded);
        var second = (await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", 2))).Value!.Steps.Single(s => s.StepNumber == 2).ExecutionId;
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", second, wrongSignature))).Outcome);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CreateDeviationAsync("B-TEST", "DEV-REJECT", "Fictional", second))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "DEV-REJECT", DeviationAction.BeginReview, "Review"))).Succeeded);
        Assert.True((await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "DEV-REJECT", DeviationAction.Reject))).Succeeded);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "DEV-REJECT", DeviationAction.Resolve, "Disposition"))).Outcome);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.ResumeAsync("B-TEST"))).Outcome);
        Assert.Equal(OperationOutcome.Forbidden, (await f.Batch(SecuredServiceFixture.Supervisor, s => s.ResumeAsync("B-TEST"))).Outcome);
        Assert.Equal(BatchStatus.Exception, (await f.Batch(SecuredServiceFixture.Operator, s => s.GetAsync("B-TEST"))).Value!.Status);
    }

    [Fact]
    public void Architecture_InnerLayersAndPresentationHaveNoReverseRuntimeReferences()
    {
        Assert.DoesNotContain(typeof(Domain.Entities.Batch).Assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.") || a.Name.Contains("Application") || a.Name.Contains("Infrastructure"));
        Assert.DoesNotContain(typeof(BatchService).Assembly.GetReferencedAssemblies(), a => a.Name!.Contains("EntityFramework") || a.Name.Contains("AspNetCore") || a.Name.Contains("Infrastructure"));
        Assert.DoesNotContain(typeof(Program).Assembly.GetReferencedAssemblies(), a => a.Name!.Contains("Infrastructure") || a.Name.Contains("EntityFramework"));
    }
}
