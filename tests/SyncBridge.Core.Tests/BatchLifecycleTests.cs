using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class BatchLifecycleTests
{
    [Fact]
    public void Constructor_SetsStatusToCreated()
    {
        var batch = CreateBatch();

        Assert.Equal(BatchStatus.Created, batch.BatchStatus);
    }

    [Fact]
    public void MarkReady_WhenCreated_SetsStatusToReady()
    {
        var batch = CreateBatch();

        batch.MarkReady();

        Assert.Equal(BatchStatus.Ready, batch.BatchStatus);
    }

    [Fact]
    public void StartProcessing_WhenReady_SetsStatusToInProgress()
    {
        var batch = CreateReadyBatch();

        StartProcessing(batch);

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void EnterException_WhenInProgress_SetsStatusToException()
    {
        var batch = CreateInProgressBatch();

        batch.EnterException(CreateDeviation(batch));

        Assert.Equal(BatchStatus.Exception, batch.BatchStatus);
    }

    [Fact]
    public void ResumeProcessing_WhenInException_SetsStatusToInProgress()
    {
        var batch = CreateExceptionBatch();
        ResolveAllDeviations(batch);

        batch.ResumeProcessing();

        Assert.Equal(BatchStatus.InProgress, batch.BatchStatus);
    }

    [Fact]
    public void Complete_WhenInProgress_SetsStatusToCompleted()
    {
        var batch = CreateInProgressBatch();

        batch.Complete();

        Assert.Equal(BatchStatus.Completed, batch.BatchStatus);
    }

    [Fact]
    public void StartProcessing_WhenCreated_ThrowsInvalidTransition()
    {
        var batch = CreateBatch();

        Assert.Throws<InvalidBatchStateTransitionException>(() => StartProcessing(batch));
    }

    [Fact]
    public void EnterException_WhenCreated_ThrowsInvalidTransition()
    {
        var batch = CreateBatch();

        Assert.Throws<InvalidBatchStateTransitionException>(() => batch.EnterException());
    }

    [Fact]
    public void Complete_WhenCreated_ThrowsInvalidTransition()
    {
        var batch = CreateBatch();

        Assert.Throws<InvalidBatchStateTransitionException>(batch.Complete);
    }

    [Fact]
    public void EnterException_WhenReady_ThrowsInvalidTransition()
    {
        var batch = CreateReadyBatch();

        Assert.Throws<InvalidBatchStateTransitionException>(() => batch.EnterException());
    }

    [Fact]
    public void Complete_WhenReady_ThrowsInvalidTransition()
    {
        var batch = CreateReadyBatch();

        Assert.Throws<InvalidBatchStateTransitionException>(batch.Complete);
    }

    [Fact]
    public void Complete_WhenInException_ThrowsInvalidTransition()
    {
        var batch = CreateExceptionBatch();

        Assert.Throws<InvalidBatchStateTransitionException>(batch.Complete);
    }

    [Fact]
    public void NormalLifecycle_ReachesCompleted()
    {
        var batch = CreateBatch();

        batch.MarkReady();
        StartProcessing(batch);
        batch.Complete();

        Assert.Equal(BatchStatus.Completed, batch.BatchStatus);
    }

    [Fact]
    public void ExceptionLifecycle_ResumesAndReachesCompleted()
    {
        var batch = CreateBatch();

        batch.MarkReady();
        StartProcessing(batch);
        batch.EnterException(CreateDeviation(batch));
        ResolveAllDeviations(batch);
        batch.ResumeProcessing();
        batch.Complete();

        Assert.Equal(BatchStatus.Completed, batch.BatchStatus);
    }

    [Fact]
    public void LifecycleMethods_WhenCompleted_RejectAllFurtherTransitions()
    {
        var batch = CreateInProgressBatch();
        batch.Complete();

        Action[] lifecycleOperations =
        [
            batch.MarkReady,
            () => StartProcessing(batch),
            () => batch.EnterException(CreateDeviation(batch)),
            batch.ResumeProcessing,
            batch.Complete
        ];

        foreach (var operation in lifecycleOperations)
        {
            Assert.Throws<InvalidBatchStateTransitionException>(operation);
            Assert.Equal(BatchStatus.Completed, batch.BatchStatus);
        }
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
            new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
    }

    private static Batch CreateReadyBatch()
    {
        var batch = CreateBatch();
        batch.MarkReady();
        return batch;
    }

    private static Batch CreateInProgressBatch()
    {
        var batch = CreateReadyBatch();
        StartProcessing(batch);
        return batch;
    }

    private static Batch CreateExceptionBatch()
    {
        var batch = CreateInProgressBatch();
        batch.EnterException(CreateDeviation(batch));
        return batch;
    }

    private static Deviation CreateDeviation(Batch batch)
    {
        return new Deviation(
            new DeviationCode("DEV-001"),
            batch.BatchNumber,
            "Unexpected manufacturing condition.",
            new OperatorId("OP-001"),
            new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
    }

    private static void ResolveAllDeviations(Batch batch)
    {
        foreach (var deviation in batch.Deviations)
        {
            deviation.BeginReview("Reviewed and acceptable.");
            deviation.Approve();
            deviation.Resolve(
                "Condition resolved; processing may resume.",
                new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero));
        }
    }

    private static void StartProcessing(Batch batch)
    {
        batch.StartProcessing(CreateRecipe(), [], []);
    }

    private static Recipe CreateRecipe()
    {
        return new Recipe(
            new RecipeId("RECIPE-ID-001"),
            new RecipeCode("RECIPE-001"),
            "Tablet Blend",
            "1.0",
            true);
    }
}
