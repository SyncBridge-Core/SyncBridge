using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class CompletedRecordProtectionTests
{
    private static readonly OperatorId Actor = new("OP-AUDIT");

    [Fact]
    public void CompletedBatch_RejectsEveryNormalBatchMutationWithoutNewAuditEvidence()
    {
        var context = CreateCompletedContext();
        var auditCount = context.Batch.AuditEvents.Count;
        var deviation = new Deviation(
            new DeviationCode("DEV-POST"), context.Batch.BatchNumber,
            "Post-completion mutation attempt.", Actor, Timestamp(8));

        Assert.Throws<InvalidBatchStateTransitionException>(
            () => context.Batch.MarkReady(Actor, Timestamp(9)));
        Assert.Throws<InvalidBatchStateTransitionException>(
            () => context.Batch.StartProcessing(context.Recipe, [], [], Actor, Timestamp(9)));
        Assert.Throws<InvalidBatchStateTransitionException>(
            () => context.Batch.EnterException(Actor, Timestamp(9), deviation));
        Assert.Throws<InvalidBatchStateTransitionException>(
            () => context.Batch.ResumeProcessing(Actor, Timestamp(9)));
        Assert.Throws<InvalidBatchStateTransitionException>(
            () => context.Batch.Complete(context.Recipe, Actor, Timestamp(9)));
        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, context.Execution, Timestamp(9), Actor));
        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CompleteStep(context.Execution, Timestamp(9), Actor));
        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CreateStepExecution(
                context.Recipe,
                context.Step,
                new BatchStepExecutionId("EXEC-POST")));

        Assert.Equal(BatchStatus.Completed, context.Batch.BatchStatus);
        Assert.Equal(auditCount, context.Batch.AuditEvents.Count);
    }

    private static (Batch Batch, Recipe Recipe, RecipeStep Step, BatchStepExecution Execution)
        CreateCompletedContext()
    {
        var batch = new Batch(
            new BatchNumber("BATCH-001"),
            new ProductionOrderNumber("PO-001"),
            new RecipeCode("RECIPE-001"),
            100m,
            new UnitOfMeasure("kg"),
            Actor,
            Timestamp(1));
        var recipe = new Recipe(
            new RecipeId("RECIPE-ID-001"), batch.RecipeCode,
            "Tablet Blend", "1.0", true);
        var step = new RecipeStep(
            new RecipeStepId("STEP-001"), recipe.RecipeId, 1,
            "Blend", "Blend the material.", false);
        recipe.AddStep(step);
        var execution = batch.CreateStepExecution(
            recipe, step, new BatchStepExecutionId("EXEC-001"));
        batch.MarkReady(Actor, Timestamp(2));
        batch.StartProcessing(recipe, [], [], Actor, Timestamp(3));
        batch.StartStep(recipe, execution, Timestamp(4), Actor);
        batch.CompleteStep(execution, Timestamp(5), Actor);
        batch.Complete(recipe, Actor, Timestamp(6));
        return (batch, recipe, step, execution);
    }

    private static DateTimeOffset Timestamp(int hour) =>
        new(2026, 10, 6, hour, 0, 0, TimeSpan.Zero);
}
