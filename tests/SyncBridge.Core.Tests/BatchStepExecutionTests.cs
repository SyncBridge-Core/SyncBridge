using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class BatchStepExecutionTests
{
    [Fact]
    public void Constructor_BeginsPendingAndPreservesIdentitiesAndStepNumber()
    {
        var executionId = new BatchStepExecutionId("EXEC-001");
        var batchNumber = new BatchNumber("BATCH-001");
        var recipeStepId = new RecipeStepId("STEP-001");

        var execution = new BatchStepExecution(executionId, batchNumber, recipeStepId, 1, false);

        Assert.Equal(executionId, execution.BatchStepExecutionId);
        Assert.Equal(batchNumber, execution.BatchNumber);
        Assert.Equal(recipeStepId, execution.RecipeStepId);
        Assert.Equal(1, execution.StepNumber);
        Assert.Equal(StepExecutionStatus.Pending, execution.StepExecutionStatus);
        Assert.Null(execution.StartedAt);
        Assert.Null(execution.CompletedAt);
        Assert.Null(execution.CompletedBy);
    }

    [Fact]
    public void Constructor_WithZeroStepNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateExecution(0));
    }

    [Fact]
    public void Constructor_WithNegativeStepNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateExecution(-1));
    }

    [Fact]
    public void Constructor_WithNullExecutionId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new BatchStepExecution(
                null!,
                new BatchNumber("BATCH-001"),
                new RecipeStepId("STEP-001"),
                1,
                false));
    }

    [Fact]
    public void Constructor_WithNullBatchNumber_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new BatchStepExecution(
                new BatchStepExecutionId("EXEC-001"),
                null!,
                new RecipeStepId("STEP-001"),
                1,
                false));
    }

    [Fact]
    public void Constructor_WithNullRecipeStepId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new BatchStepExecution(
                new BatchStepExecutionId("EXEC-001"),
                new BatchNumber("BATCH-001"),
                null!,
                1,
                false));
    }

    [Fact]
    public void StartStep_WhenPending_SetsInProgressAndStartedAt()
    {
        var context = CreateInProgressContext();
        var execution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.Step,
            new BatchStepExecutionId("EXEC-001"));
        var startedAt = new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero);

        context.Batch.StartStep(context.Recipe, execution, startedAt);

        Assert.Equal(StepExecutionStatus.InProgress, execution.StepExecutionStatus);
        Assert.Equal(startedAt, execution.StartedAt);
    }

    [Fact]
    public void StartStep_WhenAlreadyInProgress_ThrowsInvalidBatchStepExecutionException()
    {
        var context = CreateInProgressContext();
        var execution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.Step,
            new BatchStepExecutionId("EXEC-001"));
        context.Batch.StartStep(context.Recipe, execution, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, execution, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void StartStep_WhenCompleted_ThrowsInvalidBatchStepExecutionException()
    {
        var context = CreateInProgressContext();
        var execution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.Step,
            new BatchStepExecutionId("EXEC-001"));
        context.Batch.StartStep(context.Recipe, execution, DateTimeOffset.UtcNow);
        context.Batch.CompleteStep(execution, DateTimeOffset.UtcNow, new OperatorId("OP-001"));

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, execution, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompleteStep_WhenInProgress_SetsCompletionEvidence()
    {
        var context = CreateInProgressContext();
        var execution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.Step,
            new BatchStepExecutionId("EXEC-001"));
        context.Batch.StartStep(context.Recipe, execution, DateTimeOffset.UtcNow);
        var completedAt = new DateTimeOffset(2026, 10, 5, 14, 0, 0, TimeSpan.Zero);
        var completedBy = new OperatorId("OP-001");

        context.Batch.CompleteStep(execution, completedAt, completedBy);

        Assert.Equal(StepExecutionStatus.Completed, execution.StepExecutionStatus);
        Assert.Equal(completedAt, execution.CompletedAt);
        Assert.Equal(completedBy, execution.CompletedBy);
    }

    [Fact]
    public void CompleteStep_WhenPending_ThrowsInvalidBatchStepExecutionException()
    {
        var context = CreateInProgressContext();
        var execution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.Step,
            new BatchStepExecutionId("EXEC-001"));

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CompleteStep(
                execution,
                DateTimeOffset.UtcNow,
                new OperatorId("OP-001")));
    }

    [Fact]
    public void CompleteStep_WhenCompleted_ThrowsInvalidBatchStepExecutionException()
    {
        var context = CreateInProgressContext();
        var execution = context.Batch.CreateStepExecution(
            context.Recipe,
            context.Step,
            new BatchStepExecutionId("EXEC-001"));
        context.Batch.StartStep(context.Recipe, execution, DateTimeOffset.UtcNow);
        context.Batch.CompleteStep(execution, DateTimeOffset.UtcNow, new OperatorId("OP-001"));

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CompleteStep(
                execution,
                DateTimeOffset.UtcNow,
                new OperatorId("OP-001")));
    }

    private static BatchStepExecution CreateExecution(int stepNumber)
    {
        return new BatchStepExecution(
            new BatchStepExecutionId("EXEC-001"),
            new BatchNumber("BATCH-001"),
            new RecipeStepId("STEP-001"),
            stepNumber,
            false);
    }

    private static (Batch Batch, Recipe Recipe, RecipeStep Step) CreateInProgressContext()
    {
        var recipe = new Recipe(
            new RecipeId("RECIPE-ID-001"),
            new RecipeCode("RECIPE-001"),
            "Tablet Blend",
            "1.0",
            true);
        var step = new RecipeStep(
            new RecipeStepId("STEP-001"),
            recipe.RecipeId,
            1,
            "Dispense",
            "Dispense the required material.",
            false);
        recipe.AddStep(step);

        var batch = CreateBatch();
        batch.MarkReady();
        batch.StartProcessing(recipe, [], []);
        return (batch, recipe, step);
    }

    private static Batch CreateBatch()
    {
        return new Batch(
            new BatchNumber("BATCH-001"),
            new ProductionOrderNumber("PO-001"),
            new RecipeCode("RECIPE-001"),
            100m,
            new UnitOfMeasure("kg"),
            new OperatorId("OP-TEST"),
            DateTimeOffset.UtcNow);
    }
}
