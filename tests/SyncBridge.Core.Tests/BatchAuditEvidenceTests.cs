using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class BatchAuditEvidenceTests
{
    private static readonly OperatorId Actor = new("OP-AUDIT");

    [Fact]
    public void CreationAndLifecycleTransitions_ProduceAttributedAuditEvidence()
    {
        var batch = CreateBatch();

        batch.MarkReady(Actor, Timestamp(2));
        batch.StartProcessing(CreateRecipe(), [], [], Actor, Timestamp(3));

        Assert.Collection(
            batch.AuditEvents,
            created => AssertBatchEvent(created, AuditEventType.BatchCreated, Timestamp(1), "Created"),
            ready => AssertBatchEvent(ready, AuditEventType.BatchMarkedReady, Timestamp(2), "Ready"),
            started => AssertBatchEvent(started, AuditEventType.BatchProcessingStarted, Timestamp(3), "InProgress"));
    }

    [Fact]
    public void InvalidTransition_DoesNotAddSuccessEvidence()
    {
        var batch = CreateBatch();
        batch.MarkReady(Actor, Timestamp(2));
        var count = batch.AuditEvents.Count;
        Assert.Throws<InvalidBatchStateTransitionException>(
            () => batch.MarkReady(Actor, Timestamp(3)));

        Assert.Equal(BatchStatus.Ready, batch.BatchStatus);
        Assert.Equal(count, batch.AuditEvents.Count);
    }

    [Fact]
    public void VerificationEvidence_IsCreatedOnceAndRetainedWhenProcessingStarts()
    {
        var batch = CreateBatch();
        var recipe = CreateRecipe();
        recipe.AddMaterialRequirement(new RecipeMaterialRequirement(
            recipe.RecipeId, new MaterialCode("MAT-001"), 1m));
        recipe.AddEquipmentRequirement(new RecipeEquipmentRequirement(
            recipe.RecipeId, new EquipmentCode("EQ-001")));
        var material = new MaterialVerification(
            batch.BatchNumber, new MaterialCode("MAT-001"), Actor, Timestamp(2), VerificationResult.Passed);
        var equipment = new EquipmentVerification(
            batch.BatchNumber, new EquipmentCode("EQ-001"), Actor, Timestamp(2), VerificationResult.Passed);
        batch.MarkReady(Actor, Timestamp(2));

        batch.StartProcessing(recipe, [material], [equipment], Actor, Timestamp(3));

        Assert.Single(batch.AuditEvents, e => e.Action == AuditEventType.MaterialVerified);
        Assert.Single(batch.AuditEvents, e => e.Action == AuditEventType.EquipmentVerified);
        Assert.Equal(VerificationResult.Passed.ToString(), material.AuditEvidence.ResultingState);
        Assert.Equal(VerificationResult.Passed.ToString(), equipment.AuditEvidence.ResultingState);
    }

    [Fact]
    public void StepStartAndCompletion_ProduceExecutionEvidence()
    {
        var context = CreateInProgressContext(requiresSignature: false);

        context.Batch.StartStep(context.Recipe, context.Execution, Timestamp(4), Actor);
        context.Batch.CompleteStep(context.Execution, Timestamp(5), Actor);

        var started = Assert.Single(context.Batch.AuditEvents, e => e.Action == AuditEventType.StepStarted);
        var completed = Assert.Single(context.Batch.AuditEvents, e => e.Action == AuditEventType.StepCompleted);
        Assert.Equal(context.Execution.BatchStepExecutionId.Value, started.AffectedEntityId);
        Assert.Equal("InProgress", started.ResultingState);
        Assert.Equal(Timestamp(4), started.OccurredAt);
        Assert.Equal(context.Execution.BatchStepExecutionId.Value, completed.AffectedEntityId);
        Assert.Equal("Completed", completed.ResultingState);
        Assert.Equal(Actor, completed.OperatorId);
    }

    [Fact]
    public void SignedStepCompletion_RetainsOneSignatureAuditEvent()
    {
        var context = CreateInProgressContext(requiresSignature: true);
        context.Batch.StartStep(context.Recipe, context.Execution, Timestamp(4), Actor);
        var signature = new ElectronicSignature(
            new ElectronicSignatureId("SIG-001"),
            context.Batch.BatchNumber,
            RelatedRecordType.BatchStepExecution,
            new RelatedRecordId(context.Execution.BatchStepExecutionId.Value),
            Actor,
            ElectronicSignatureMeaning.Completion,
            Timestamp(5));

        context.Batch.CompleteStep(context.Execution, Timestamp(6), Actor, signature);

        var evidence = Assert.Single(
            context.Batch.AuditEvents,
            e => e.Action == AuditEventType.ElectronicSignatureApplied);
        Assert.Equal(signature.ElectronicSignatureId.Value, evidence.AffectedEntityId);
        Assert.Equal(Actor, evidence.OperatorId);
        Assert.Equal(Timestamp(5), evidence.OccurredAt);
    }

    [Fact]
    public void DeviationLifecycleAndExceptionResume_ProduceDistinctEvidence()
    {
        var batch = CreateInProgressContext(false).Batch;
        var deviation = new Deviation(
            new DeviationCode("DEV-001"), batch.BatchNumber,
            "Temperature excursion.", Actor, Timestamp(4));

        batch.EnterException(Actor, Timestamp(5), deviation);
        deviation.BeginReview("Investigation complete.", Actor, Timestamp(6));
        deviation.Approve(Actor, Timestamp(7));
        deviation.Resolve("Accepted after adjustment.", Timestamp(8), Actor);
        batch.ResumeProcessing(Actor, Timestamp(9));

        var actions = batch.AuditEvents.Select(e => e.Action).ToArray();
        Assert.Contains(AuditEventType.DeviationOpened, actions);
        Assert.Contains(AuditEventType.DeviationReviewStarted, actions);
        Assert.Contains(AuditEventType.DeviationApproved, actions);
        Assert.Contains(AuditEventType.DeviationResolved, actions);
        Assert.Contains(AuditEventType.BatchExceptionEntered, actions);
        Assert.Contains(AuditEventType.BatchProcessingResumed, actions);
        Assert.Single(actions, action => action == AuditEventType.DeviationOpened);
    }

    [Fact]
    public void RejectedResume_DoesNotProduceResumeEvidence()
    {
        var batch = CreateInProgressContext(false).Batch;
        var deviation = new Deviation(
            new DeviationCode("DEV-001"), batch.BatchNumber,
            "Temperature excursion.", Actor, Timestamp(4));
        batch.EnterException(Actor, Timestamp(5), deviation);

        Assert.Throws<BatchDeviationException>(() => batch.ResumeProcessing(Actor, Timestamp(6)));

        Assert.Equal(BatchStatus.Exception, batch.BatchStatus);
        Assert.DoesNotContain(batch.AuditEvents, e => e.Action == AuditEventType.BatchProcessingResumed);
    }

    [Fact]
    public void SuccessfulCompletion_ProducesAttributedCompletedEvidence()
    {
        var context = CreateInProgressContext(false);
        context.Batch.StartStep(context.Recipe, context.Execution, Timestamp(4), Actor);
        context.Batch.CompleteStep(context.Execution, Timestamp(5), Actor);

        context.Batch.Complete(context.Recipe, Actor, Timestamp(6));

        var evidence = Assert.Single(
            context.Batch.AuditEvents,
            e => e.Action == AuditEventType.BatchCompleted);
        AssertBatchEvent(evidence, AuditEventType.BatchCompleted, Timestamp(6), "Completed");
    }

    [Fact]
    public void IncompleteOrSignatureGatedStep_BlocksCompletionWithoutSuccessEvidence()
    {
        var context = CreateInProgressContext(true);
        context.Batch.StartStep(context.Recipe, context.Execution, Timestamp(4), Actor);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.Complete(context.Recipe, Actor, Timestamp(5)));

        Assert.Equal(BatchStatus.InProgress, context.Batch.BatchStatus);
        Assert.DoesNotContain(context.Batch.AuditEvents, e => e.Action == AuditEventType.BatchCompleted);
    }

    [Fact]
    public void UnresolvedDeviation_BlocksCompletionWithoutSuccessEvidence()
    {
        var context = CreateInProgressContext(false);
        var deviation = new Deviation(
            new DeviationCode("DEV-001"), context.Batch.BatchNumber,
            "Temperature excursion.", Actor, Timestamp(4));
        context.Batch.EnterException(Actor, Timestamp(5), deviation);

        Assert.Throws<InvalidBatchStateTransitionException>(
            () => context.Batch.Complete(context.Recipe, Actor, Timestamp(6)));

        Assert.Equal(BatchStatus.Exception, context.Batch.BatchStatus);
        Assert.DoesNotContain(context.Batch.AuditEvents, e => e.Action == AuditEventType.BatchCompleted);
    }

    private static (Batch Batch, Recipe Recipe, BatchStepExecution Execution) CreateInProgressContext(
        bool requiresSignature)
    {
        var batch = CreateBatch();
        var recipe = CreateRecipe();
        var step = new RecipeStep(
            new RecipeStepId("STEP-001"), recipe.RecipeId, 1,
            "Blend", "Blend the material.", requiresSignature);
        recipe.AddStep(step);
        var execution = batch.CreateStepExecution(
            recipe, step, new BatchStepExecutionId("EXEC-001"));
        batch.MarkReady(Actor, Timestamp(2));
        batch.StartProcessing(recipe, [], [], Actor, Timestamp(3));
        return (batch, recipe, execution);
    }

    private static Batch CreateBatch() => new(
        new BatchNumber("BATCH-001"),
        new ProductionOrderNumber("PO-001"),
        new RecipeCode("RECIPE-001"),
        100m,
        new UnitOfMeasure("kg"),
        Actor,
        Timestamp(1));

    private static Recipe CreateRecipe() => new(
        new RecipeId("RECIPE-ID-001"),
        new RecipeCode("RECIPE-001"),
        "Tablet Blend",
        "1.0",
        true);

    private static void AssertBatchEvent(
        AuditEvent evidence,
        AuditEventType action,
        DateTimeOffset occurredAt,
        string resultingState)
    {
        Assert.Equal(action, evidence.Action);
        Assert.Equal(Actor, evidence.OperatorId);
        Assert.Equal("Batch", evidence.AffectedEntity);
        Assert.Equal("BATCH-001", evidence.AffectedEntityId);
        Assert.Equal(occurredAt, evidence.OccurredAt);
        Assert.Equal(resultingState, evidence.ResultingState);
    }

    private static DateTimeOffset Timestamp(int hour) =>
        new(2026, 10, 6, hour, 0, 0, TimeSpan.Zero);
}
