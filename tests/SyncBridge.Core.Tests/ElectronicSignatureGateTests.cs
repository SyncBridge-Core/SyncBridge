using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class ElectronicSignatureGateTests
{
    [Fact]
    public void CompleteStep_WhenSignatureNotRequired_CompletesWithoutSignature()
    {
        var context = CreateContext(false);

        context.Batch.CompleteStep(context.Execution, Timestamp(2), new OperatorId("OP-001"));

        Assert.Equal(StepExecutionStatus.Completed, context.Execution.StepExecutionStatus);
    }

    [Fact]
    public void CompleteStep_WhenSignatureRequiredAndMatches_CompletesStep()
    {
        var context = CreateContext(true);

        context.Batch.CompleteStep(
            context.Execution,
            Timestamp(2),
            new OperatorId("OP-001"),
            CreateSignature(context.Batch.BatchNumber, context.Execution.BatchStepExecutionId));

        Assert.Equal(StepExecutionStatus.Completed, context.Execution.StepExecutionStatus);
        Assert.Equal(Timestamp(2), context.Execution.CompletedAt);
    }

    [Fact]
    public void CompleteStep_WhenRequiredSignatureMissing_RejectsWithoutMutatingExecution()
    {
        var context = CreateContext(true);

        Assert.Throws<ElectronicSignatureValidationException>(
            () => context.Batch.CompleteStep(
                context.Execution,
                Timestamp(2),
                new OperatorId("OP-001")));

        AssertIncomplete(context.Execution);
    }

    [Fact]
    public void CompleteStep_WhenSignatureBelongsToAnotherBatch_RejectsWithoutMutatingExecution()
    {
        var context = CreateContext(true);
        var signature = CreateSignature(new BatchNumber("BATCH-OTHER"), context.Execution.BatchStepExecutionId);

        AssertSignatureRejected(context, signature);
    }

    [Fact]
    public void CompleteStep_WhenSignatureTargetsAnotherExecution_RejectsWithoutMutatingExecution()
    {
        var context = CreateContext(true);
        var signature = CreateSignature(context.Batch.BatchNumber, new BatchStepExecutionId("EXEC-OTHER"));

        AssertSignatureRejected(context, signature);
    }

    [Fact]
    public void CompleteStep_WhenSignatureTargetsWrongRecordType_RejectsWithoutMutatingExecution()
    {
        var context = CreateContext(true);
        var signature = CreateSignature(
            context.Batch.BatchNumber,
            context.Execution.BatchStepExecutionId,
            new RelatedRecordType("OtherControlledRecord"));

        AssertSignatureRejected(context, signature);
    }

    [Fact]
    public void SignatureEvidence_DoesNotBypassRecipeStepOrdering()
    {
        var context = CreateTwoStepContext();
        var secondExecution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.SecondStep,
            new BatchStepExecutionId("EXEC-002"));

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, secondExecution, Timestamp(2)));
        Assert.Equal(StepExecutionStatus.Pending, secondExecution.StepExecutionStatus);
    }

    [Theory]
    [InlineData(BatchStatus.Created)]
    [InlineData(BatchStatus.Ready)]
    [InlineData(BatchStatus.Exception)]
    [InlineData(BatchStatus.Completed)]
    public void CompleteStep_OutsideInProgressBatch_IsRejected(BatchStatus batchStatus)
    {
        var context = CreateContextForStatus(batchStatus);
        var signature = CreateSignature(context.Batch.BatchNumber, context.Execution.BatchStepExecutionId);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CompleteStep(
                context.Execution,
                Timestamp(2),
                new OperatorId("OP-001"),
                signature));
    }

    [Fact]
    public void CompleteStep_DoesNotMutateAcceptedSignatureEvidence()
    {
        var context = CreateContext(true);
        var signature = CreateSignature(context.Batch.BatchNumber, context.Execution.BatchStepExecutionId);
        var expected = (
            signature.ElectronicSignatureId,
            signature.BatchNumber,
            signature.RelatedRecordType,
            signature.RelatedRecordId,
            signature.OperatorId,
            signature.Meaning,
            signature.SignedAt);

        context.Batch.CompleteStep(
            context.Execution,
            Timestamp(2),
            new OperatorId("OP-002"),
            signature);

        Assert.Equal(expected, (
            signature.ElectronicSignatureId,
            signature.BatchNumber,
            signature.RelatedRecordType,
            signature.RelatedRecordId,
            signature.OperatorId,
            signature.Meaning,
            signature.SignedAt));
    }

    [Fact]
    public void CompleteStep_WhenAlreadyCompleted_RemainsRejectedWithValidSignature()
    {
        var context = CreateContext(true);
        var signature = CreateSignature(context.Batch.BatchNumber, context.Execution.BatchStepExecutionId);
        context.Batch.CompleteStep(context.Execution, Timestamp(2), new OperatorId("OP-001"), signature);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CompleteStep(
                context.Execution,
                Timestamp(3),
                new OperatorId("OP-001"),
                signature));
    }

    private static void AssertSignatureRejected(
        (Batch Batch, Recipe Recipe, BatchStepExecution Execution) context,
        ElectronicSignature signature)
    {
        Assert.Throws<ElectronicSignatureValidationException>(
            () => context.Batch.CompleteStep(
                context.Execution,
                Timestamp(2),
                new OperatorId("OP-001"),
                signature));
        AssertIncomplete(context.Execution);
    }

    private static void AssertIncomplete(BatchStepExecution execution)
    {
        Assert.Equal(StepExecutionStatus.InProgress, execution.StepExecutionStatus);
        Assert.Null(execution.CompletedAt);
        Assert.Null(execution.CompletedBy);
    }

    private static ElectronicSignature CreateSignature(
        BatchNumber batchNumber,
        BatchStepExecutionId executionId,
        RelatedRecordType? recordType = null) =>
        new(
            new ElectronicSignatureId("SIG-001"),
            batchNumber,
            recordType ?? RelatedRecordType.BatchStepExecution,
            new RelatedRecordId(executionId.Value),
            new OperatorId("SIGNER-001"),
            ElectronicSignatureMeaning.Completion,
            Timestamp(1));

    private static (Batch Batch, Recipe Recipe, BatchStepExecution Execution) CreateContext(
        bool requiresSignature)
    {
        var recipe = CreateRecipe();
        var step = AddStep(recipe, 1, requiresSignature);
        var batch = CreateBatch();
        batch.MarkReady();
        batch.StartProcessing(recipe, [], []);
        var execution = batch.CreateStepExecution(recipe, step, new BatchStepExecutionId("EXEC-001"));
        batch.StartStep(recipe, execution, Timestamp(0));
        return (batch, recipe, execution);
    }

    private static (Batch Batch, Recipe Recipe, RecipeStep SecondStep) CreateTwoStepContext()
    {
        var recipe = CreateRecipe();
        var firstStep = AddStep(recipe, 1, true);
        var secondStep = AddStep(recipe, 2, true);
        var batch = CreateBatch();
        batch.MarkReady();
        batch.StartProcessing(recipe, [], []);
        var firstExecution = batch.CreateStepExecution(recipe, firstStep, new BatchStepExecutionId("EXEC-001"));
        batch.StartStep(recipe, firstExecution, Timestamp(0));
        return (batch, recipe, secondStep);
    }

    private static (Batch Batch, Recipe Recipe, BatchStepExecution Execution) CreateContextForStatus(
        BatchStatus status)
    {
        var recipe = CreateRecipe();
        var step = AddStep(recipe, 1, true);
        var batch = CreateBatch();
        var execution = batch.CreateStepExecution(recipe, step, new BatchStepExecutionId("EXEC-001"));

        if (status != BatchStatus.Created)
        {
            batch.MarkReady();
        }

        if (status is BatchStatus.Exception or BatchStatus.Completed)
        {
            batch.StartProcessing(recipe, [], []);
            batch.StartStep(recipe, execution, Timestamp(0));

            if (status == BatchStatus.Exception)
            {
                batch.EnterException(new Deviation(
                    new DeviationCode("DEV-001"),
                    batch.BatchNumber,
                    "Unexpected manufacturing condition.",
                    new OperatorId("OP-001"),
                    Timestamp(0)));
            }
            else
            {
                batch.Complete();
            }
        }

        return (batch, recipe, execution);
    }

    private static Recipe CreateRecipe() =>
        new(new RecipeId("RECIPE-ID-001"), new RecipeCode("RECIPE-001"), "Tablet Blend", "1.0", true);

    private static RecipeStep AddStep(Recipe recipe, int stepNumber, bool requiresSignature)
    {
        var step = new RecipeStep(
            new RecipeStepId($"STEP-{stepNumber:000}"),
            recipe.RecipeId,
            stepNumber,
            $"Step {stepNumber}",
            $"Perform step {stepNumber}.",
            requiresSignature);
        recipe.AddStep(step);
        return step;
    }

    private static Batch CreateBatch() =>
        new(
            new BatchNumber("BATCH-001"),
            new ProductionOrderNumber("PO-001"),
            new RecipeCode("RECIPE-001"),
            100m,
            new UnitOfMeasure("kg"),
            new OperatorId("OP-TEST"),
            Timestamp(0));

    private static DateTimeOffset Timestamp(int hourOffset) =>
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero).AddHours(hourOffset);
}
