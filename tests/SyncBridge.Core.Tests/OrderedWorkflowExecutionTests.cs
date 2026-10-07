using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class OrderedWorkflowExecutionTests
{
    [Fact]
    public void ThreeStepRecipe_ExecutesInPrescribedOrder()
    {
        var context = CreateContext(3);
        var executions = RegisterExecutions(context);

        for (var index = 0; index < executions.Count; index++)
        {
            context.Batch.StartStep(context.Recipe, executions[index], Timestamp(index));
            context.Batch.CompleteStep(
                executions[index],
                Timestamp(index + 1),
                new OperatorId("OP-001"));
        }

        Assert.All(
            executions,
            execution => Assert.Equal(StepExecutionStatus.Completed, execution.StepExecutionStatus));
    }

    [Fact]
    public void StartStep_TwoBeforeStepOneCompletes_IsRejected()
    {
        var context = CreateContext(2);
        var executions = RegisterExecutions(context);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, executions[1], Timestamp(1)));
        Assert.Equal(StepExecutionStatus.Pending, executions[1].StepExecutionStatus);
    }

    [Fact]
    public void StartStep_ThreeBeforeStepTwoCompletes_IsRejected()
    {
        var context = CreateContext(3);
        var executions = RegisterExecutions(context);
        context.Batch.StartStep(context.Recipe, executions[0], Timestamp(1));
        context.Batch.CompleteStep(executions[0], Timestamp(2), new OperatorId("OP-001"));

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, executions[2], Timestamp(3)));
        Assert.Equal(StepExecutionStatus.Pending, executions[2].StepExecutionStatus);
    }

    [Fact]
    public void StartStep_WhileAnotherStepIsInProgress_IsRejected()
    {
        var context = CreateContext(2);
        var executions = RegisterExecutions(context);
        context.Batch.StartStep(context.Recipe, executions[0], Timestamp(1));

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, executions[1], Timestamp(2)));
        Assert.Equal(StepExecutionStatus.Pending, executions[1].StepExecutionStatus);
    }

    [Fact]
    public void CreateStepExecution_WithStepFromAnotherRecipe_IsRejected()
    {
        var context = CreateContext(1);
        var otherRecipe = CreateRecipe("RECIPE-ID-OTHER", "RECIPE-OTHER", 1);
        var foreignStep = Assert.Single(otherRecipe.Steps);

        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.CreateStepExecution(
                context.Recipe,
                foreignStep,
                new BatchStepExecutionId("EXEC-001")));
    }

    [Fact]
    public void StartStep_WhenBatchCreated_IsRejected()
    {
        var context = CreateContext(1, startBatch: false);
        var execution = RegisterExecutions(context)[0];

        AssertStepStartRejected(context, execution);
    }

    [Fact]
    public void StartStep_WhenBatchReady_IsRejected()
    {
        var context = CreateContext(1, startBatch: false);
        context.Batch.MarkReady();
        var execution = RegisterExecutions(context)[0];

        AssertStepStartRejected(context, execution);
    }

    [Fact]
    public void StartStep_WhenBatchException_IsRejected()
    {
        var context = CreateContext(1);
        var execution = RegisterExecutions(context)[0];
        context.Batch.EnterException(CreateDeviation(context.Batch));

        AssertStepStartRejected(context, execution);
    }

    [Fact]
    public void StartStep_WhenBatchCompleted_IsRejected()
    {
        var context = CreateContext(1);
        var execution = RegisterExecutions(context)[0];
        context.Batch.Complete();

        AssertStepStartRejected(context, execution);
    }

    [Fact]
    public void StartStep_WhenBatchInProgress_StartsEligibleStep()
    {
        var context = CreateContext(1);
        var execution = RegisterExecutions(context)[0];

        context.Batch.StartStep(context.Recipe, execution, Timestamp(1));

        Assert.Equal(StepExecutionStatus.InProgress, execution.StepExecutionStatus);
    }

    [Fact]
    public void CompleteFinalStep_DoesNotCompleteBatch()
    {
        var context = CreateContext(1);
        var execution = RegisterExecutions(context)[0];
        context.Batch.StartStep(context.Recipe, execution, Timestamp(1));

        context.Batch.CompleteStep(execution, Timestamp(2), new OperatorId("OP-001"));

        Assert.Equal(StepExecutionStatus.Completed, execution.StepExecutionStatus);
        Assert.Equal(BatchStatus.InProgress, context.Batch.BatchStatus);
    }

    [Fact]
    public void CompleteStep_WhenDefinitionDoesNotRequireSignature_DoesNotRequireSignature()
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
            "Approval",
            "Review the step.",
            false);
        recipe.AddStep(step);
        var batch = CreateBatch();
        batch.MarkReady();
        batch.StartProcessing(recipe, [], []);
        var execution = batch.CreateStepExecution(
            recipe,
            step,
            new BatchStepExecutionId("EXEC-001"));
        batch.StartStep(recipe, execution, Timestamp(1));

        batch.CompleteStep(execution, Timestamp(2), new OperatorId("OP-001"));

        Assert.Equal(StepExecutionStatus.Completed, execution.StepExecutionStatus);
    }

    private static void AssertStepStartRejected(
        (Batch Batch, Recipe Recipe, IReadOnlyList<RecipeStep> Steps) context,
        BatchStepExecution execution)
    {
        Assert.Throws<InvalidBatchStepExecutionException>(
            () => context.Batch.StartStep(context.Recipe, execution, Timestamp(1)));
        Assert.Equal(StepExecutionStatus.Pending, execution.StepExecutionStatus);
    }

    private static List<BatchStepExecution> RegisterExecutions(
        (Batch Batch, Recipe Recipe, IReadOnlyList<RecipeStep> Steps) context)
    {
        return context.Steps
            .Select(step => context.Batch.CreateStepExecution(
                context.Recipe,
                step,
                new BatchStepExecutionId($"EXEC-{step.StepNumber:000}")))
            .ToList();
    }

    private static (Batch Batch, Recipe Recipe, IReadOnlyList<RecipeStep> Steps) CreateContext(
        int stepCount,
        bool startBatch = true)
    {
        var recipe = CreateRecipe("RECIPE-ID-001", "RECIPE-001", stepCount);
        var batch = CreateBatch();

        if (startBatch)
        {
            batch.MarkReady();
            batch.StartProcessing(recipe, [], []);
        }

        return (batch, recipe, recipe.Steps);
    }

    private static Recipe CreateRecipe(string recipeId, string recipeCode, int stepCount)
    {
        var recipe = new Recipe(
            new RecipeId(recipeId),
            new RecipeCode(recipeCode),
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
                false));
        }

        return recipe;
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
            Timestamp(0));
    }

    private static Deviation CreateDeviation(Batch batch)
    {
        return new Deviation(
            new DeviationCode("DEV-001"),
            batch.BatchNumber,
            "Unexpected manufacturing condition.",
            new OperatorId("OP-001"),
            Timestamp(0));
    }

    private static DateTimeOffset Timestamp(int hourOffset)
    {
        return new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero)
            .AddHours(hourOffset);
    }
}
