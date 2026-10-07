using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class BatchDeviationLifecycleTests
{
    [Fact]
    public void EnterException_WithSameBatchDeviation_AssociatesDeviationAndSetsException()
    {
        var batch = CreateInProgressBatch();
        var deviation = CreateDeviation(batch, "DEV-001");

        batch.EnterException(deviation);

        Assert.Equal(BatchStatus.Exception, batch.BatchStatus);
        Assert.Same(deviation, Assert.Single(batch.Deviations));
    }

    [Fact]
    public void EnterException_WithoutDeviation_RejectsAndLeavesBatchInProgress()
    {
        var batch = CreateInProgressBatch();

        Assert.Throws<BatchDeviationException>(() => batch.EnterException());
        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
        Assert.Empty(batch.Deviations);
    }

    [Fact]
    public void EnterException_WithWrongBatchDeviation_RejectsAndLeavesBatchInProgress()
    {
        var batch = CreateInProgressBatch();
        var deviation = CreateDeviation(CreateBatch("BATCH-OTHER"), "DEV-001");

        Assert.Throws<BatchDeviationException>(() => batch.EnterException(deviation));
        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
        Assert.Empty(batch.Deviations);
    }

    [Fact]
    public void EnterException_WithAlreadyResolvedDeviation_RejectsAndLeavesBatchInProgress()
    {
        var batch = CreateInProgressBatch();
        var deviation = CreateDeviation(batch, "DEV-001");
        Resolve(deviation);

        Assert.Throws<BatchDeviationException>(() => batch.EnterException(deviation));
        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
        Assert.Empty(batch.Deviations);
    }

    [Theory]
    [InlineData(BatchStatus.Created)]
    [InlineData(BatchStatus.Ready)]
    [InlineData(BatchStatus.Exception)]
    [InlineData(BatchStatus.Completed)]
    public void EnterException_FromInvalidBatchState_IsRejected(BatchStatus status)
    {
        var batch = CreateBatchInStatus(status);
        var initialCount = batch.Deviations.Count;

        Assert.Throws<InvalidBatchStateTransitionException>(
            () => batch.EnterException(CreateDeviation(batch, "DEV-NEW")));
        Assert.Equal(status, batch.BatchStatus);
        Assert.Equal(initialCount, batch.Deviations.Count);
    }

    [Fact]
    public void ResumeProcessing_WithUnresolvedDeviation_IsRejected()
    {
        var batch = CreateInProgressBatch();
        batch.EnterException(CreateDeviation(batch, "DEV-001"));

        Assert.Throws<BatchDeviationException>(batch.ResumeProcessing);
        Assert.Equal(BatchStatus.Exception, batch.BatchStatus);
    }

    [Fact]
    public void ResumeProcessing_AfterDeviationResolution_ReturnsToInProgress()
    {
        var batch = CreateInProgressBatch();
        var deviation = CreateDeviation(batch, "DEV-001");
        batch.EnterException(deviation);
        Resolve(deviation);

        batch.ResumeProcessing();

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void WrongBatchResolvedDeviation_CannotSatisfyResumptionGate()
    {
        var batch = CreateInProgressBatch();
        var blockingDeviation = CreateDeviation(batch, "DEV-001");
        batch.EnterException(blockingDeviation);
        var foreignDeviation = CreateDeviation(CreateBatch("BATCH-OTHER"), "DEV-OTHER");
        Resolve(foreignDeviation);

        Assert.Throws<BatchDeviationException>(batch.ResumeProcessing);
        Assert.Equal(BatchStatus.Exception, batch.BatchStatus);
        Assert.True(blockingDeviation.IsBlocking);
    }

    [Fact]
    public void ResumeProcessing_WithOneOfTwoDeviationsUnresolved_IsRejected()
    {
        var batch = CreateInProgressBatch();
        var first = CreateDeviation(batch, "DEV-001");
        var second = CreateDeviation(batch, "DEV-002");
        batch.EnterException(first, second);
        Resolve(first);

        Assert.Throws<BatchDeviationException>(batch.ResumeProcessing);
        Assert.Equal(BatchStatus.Exception, batch.BatchStatus);
    }

    [Fact]
    public void ResumeProcessing_WhenAllAssociatedDeviationsResolved_Succeeds()
    {
        var batch = CreateInProgressBatch();
        var first = CreateDeviation(batch, "DEV-001");
        var second = CreateDeviation(batch, "DEV-002");
        batch.EnterException(first, second);
        Resolve(first);
        Resolve(second);

        batch.ResumeProcessing();

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
        Assert.Equal(2, batch.Deviations.Count);
    }

    [Fact]
    public void OrderedExecution_RemainsBlockedInExceptionAndEnforcedAfterResumption()
    {
        var recipe = CreateRecipe(false, stepCount: 2);
        var batch = CreateInProgressBatch(recipe);
        var firstExecution = batch.CreateStepExecution(
            recipe,
            recipe.Steps[0],
            new BatchStepExecutionId("EXEC-001"));
        var secondExecution = batch.CreateStepExecution(
            recipe,
            recipe.Steps[1],
            new BatchStepExecutionId("EXEC-002"));
        var deviation = CreateDeviation(batch, "DEV-001");
        batch.EnterException(deviation);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => batch.StartStep(recipe, firstExecution, Timestamp(1)));

        Resolve(deviation);
        batch.ResumeProcessing();

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => batch.StartStep(recipe, secondExecution, Timestamp(1)));
        batch.StartStep(recipe, firstExecution, Timestamp(1));
        Assert.Equal(StepExecutionStatus.InProgress, firstExecution.StepExecutionStatus);
    }

    [Fact]
    public void SignatureGate_RemainsRequiredAfterResumption()
    {
        var recipe = CreateRecipe(true);
        var batch = CreateInProgressBatch(recipe);
        var execution = batch.CreateStepExecution(
            recipe,
            recipe.Steps[0],
            new BatchStepExecutionId("EXEC-001"));
        batch.StartStep(recipe, execution, Timestamp(1));
        var deviation = CreateDeviation(batch, "DEV-001", execution);
        batch.EnterException(deviation);
        Resolve(deviation);
        batch.ResumeProcessing();

        Assert.Throws<ElectronicSignatureValidationException>(
            () => batch.CompleteStep(execution, Timestamp(3), new OperatorId("OP-001")));
        Assert.Equal(StepExecutionStatus.InProgress, execution.StepExecutionStatus);
    }

    [Fact]
    public void ResumeProcessing_DoesNotCompleteBatchOrActiveStep()
    {
        var recipe = CreateRecipe(false);
        var batch = CreateInProgressBatch(recipe);
        var execution = batch.CreateStepExecution(
            recipe,
            recipe.Steps[0],
            new BatchStepExecutionId("EXEC-001"));
        batch.StartStep(recipe, execution, Timestamp(1));
        var deviation = CreateDeviation(batch, "DEV-001", execution);
        batch.EnterException(deviation);
        Resolve(deviation);

        batch.ResumeProcessing();

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
        Assert.Equal(StepExecutionStatus.InProgress, execution.StepExecutionStatus);
        Assert.Null(execution.CompletedAt);
    }

    private static Batch CreateBatchInStatus(BatchStatus status)
    {
        var batch = CreateBatch();

        if (status != BatchStatus.Created)
        {
            batch.MarkReady();
        }

        if (status is BatchStatus.Exception or BatchStatus.Completed)
        {
            StartProcessing(batch, CreateRecipe(false));

            if (status == BatchStatus.Exception)
            {
                batch.EnterException(CreateDeviation(batch, "DEV-001"));
            }
            else
            {
                batch.Complete();
            }
        }

        return batch;
    }

    private static Batch CreateInProgressBatch(Recipe? recipe = null)
    {
        var batch = CreateBatch();
        batch.MarkReady();
        StartProcessing(batch, recipe ?? CreateRecipe(false));
        return batch;
    }

    private static Batch CreateBatch(string batchNumber = "BATCH-001") =>
        new(
            new BatchNumber(batchNumber),
            new ProductionOrderNumber("PO-001"),
            new RecipeCode("RECIPE-001"),
            100m,
            new UnitOfMeasure("kg"),
            new OperatorId("OP-TEST"),
            Timestamp(0));

    private static void StartProcessing(Batch batch, Recipe recipe) =>
        batch.StartProcessing(recipe, [], []);

    private static Recipe CreateRecipe(bool requiresSignature, int stepCount = 1)
    {
        var recipe = new Recipe(
            new RecipeId("RECIPE-ID-001"),
            new RecipeCode("RECIPE-001"),
            "Tablet Blend",
            "1.0",
            true);

        for (var stepNumber = 1; stepNumber <= stepCount; stepNumber++)
        {
            recipe.AddStep(new RecipeStep(
                new RecipeStepId($"STEP-{stepNumber:000}"),
                recipe.RecipeId,
                stepNumber,
                $"Step {stepNumber}",
                $"Perform step {stepNumber}.",
                requiresSignature));
        }

        return recipe;
    }

    private static Deviation CreateDeviation(
        Batch batch,
        string code,
        BatchStepExecution? execution = null) =>
        new(
            new DeviationCode(code),
            batch.BatchNumber,
            execution,
            "Unexpected manufacturing condition.",
            new OperatorId("OP-001"),
            Timestamp(0));

    private static void Resolve(Deviation deviation)
    {
        deviation.BeginReview("Quality review completed.");
        deviation.Approve();
        deviation.Resolve("Condition resolved; processing may resume.", Timestamp(2));
    }

    private static DateTimeOffset Timestamp(int hourOffset) =>
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero).AddHours(hourOffset);
}
