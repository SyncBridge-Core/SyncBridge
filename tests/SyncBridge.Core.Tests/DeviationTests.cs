using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class DeviationTests
{
    [Fact]
    public void Constructor_AtBatchLevel_PreservesEvidenceAndBeginsOpen()
    {
        var code = new DeviationCode("DEV-001");
        var batchNumber = new BatchNumber("BATCH-001");
        var reportedBy = new OperatorId("OP-001");
        var openedAt = Timestamp(0);

        var deviation = new Deviation(
            code,
            batchNumber,
            "Unexpected temperature excursion.",
            reportedBy,
            openedAt);

        Assert.Equal(code, deviation.DeviationCode);
        Assert.Equal(batchNumber, deviation.BatchNumber);
        Assert.Null(deviation.BatchStepExecutionId);
        Assert.Equal("Unexpected temperature excursion.", deviation.Description);
        Assert.Equal(reportedBy, deviation.ReportedBy);
        Assert.Equal(openedAt, deviation.OpenedAt);
        Assert.Equal(DeviationStatus.Open, deviation.DeviationStatus);
        Assert.True(deviation.IsBlocking);
    }

    [Fact]
    public void Constructor_WithSameBatchStepExecution_PreservesStepIdentity()
    {
        var execution = CreateExecution("BATCH-001");

        var deviation = CreateDeviation("DEV-001", "BATCH-001", execution);

        Assert.Equal(execution.BatchStepExecutionId, deviation.BatchStepExecutionId);
    }

    [Fact]
    public void Constructor_WithWrongBatchStepExecution_ThrowsDomainException()
    {
        var execution = CreateExecution("BATCH-OTHER");

        Assert.Throws<DomainException>(
            () => CreateDeviation("DEV-001", "BATCH-001", execution));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithMissingDescription_ThrowsArgumentException(string description)
    {
        Assert.Throws<ArgumentException>(
            () => new Deviation(
                new DeviationCode("DEV-001"),
                new BatchNumber("BATCH-001"),
                description,
                new OperatorId("OP-001"),
                Timestamp(0)));
    }

    [Fact]
    public void BatchOrigin_HasNoPublicMutationPath()
    {
        var property = typeof(Deviation).GetProperty(nameof(Deviation.BatchNumber));

        Assert.NotNull(property);
        Assert.False(property!.CanWrite);
    }

    [Fact]
    public void BeginReview_WhenOpen_PreservesCommentsAndSetsUnderReview()
    {
        var deviation = CreateDeviation();

        deviation.BeginReview("Quality review completed.");

        Assert.Equal("Quality review completed.", deviation.ReviewComments);
        Assert.Equal(DeviationStatus.UnderReview, deviation.DeviationStatus);
        Assert.True(deviation.IsBlocking);
    }

    [Fact]
    public void BeginReview_WithBlankComments_RejectsWithoutChangingStatus()
    {
        var deviation = CreateDeviation();

        Assert.Throws<ArgumentException>(() => deviation.BeginReview(" "));
        Assert.Equal(DeviationStatus.Open, deviation.DeviationStatus);
        Assert.Null(deviation.ReviewComments);
    }

    [Fact]
    public void Approve_WhenUnderReview_SetsApprovedAndRemainsBlocking()
    {
        var deviation = CreateDeviation();
        deviation.BeginReview("Acceptable after investigation.");

        deviation.Approve();

        Assert.Equal(DeviationStatus.Approved, deviation.DeviationStatus);
        Assert.True(deviation.IsBlocking);
    }

    [Fact]
    public void Reject_WhenUnderReview_PreservesRejectedReviewResultAndRemainsBlocking()
    {
        var deviation = CreateDeviation();
        deviation.BeginReview("Evidence is insufficient.");

        deviation.Reject();

        Assert.Equal(DeviationStatus.Rejected, deviation.DeviationStatus);
        Assert.True(deviation.IsBlocking);
    }

    [Fact]
    public void Resolve_WhenApproved_PreservesDispositionAndClosesDeviation()
    {
        var deviation = CreateApprovedDeviation();

        deviation.Resolve("Equipment adjusted and result accepted.", Timestamp(2));

        Assert.Equal("Equipment adjusted and result accepted.", deviation.FinalDisposition);
        Assert.Equal(Timestamp(2), deviation.ResolvedAt);
        Assert.Equal(DeviationStatus.Closed, deviation.DeviationStatus);
        Assert.False(deviation.IsBlocking);
    }

    [Fact]
    public void Resolve_WithoutDisposition_RejectsWithoutResolving()
    {
        var deviation = CreateApprovedDeviation();

        Assert.Throws<ArgumentException>(() => deviation.Resolve(" ", Timestamp(2)));
        Assert.Equal(DeviationStatus.Approved, deviation.DeviationStatus);
        Assert.Null(deviation.FinalDisposition);
        Assert.Null(deviation.ResolvedAt);
        Assert.True(deviation.IsBlocking);
    }

    [Fact]
    public void Resolve_WhenAlreadyClosed_RejectsWithoutOverwritingEvidence()
    {
        var deviation = CreateApprovedDeviation();
        deviation.Resolve("Original disposition.", Timestamp(2));

        Assert.Throws<InvalidDeviationStateTransitionException>(
            () => deviation.Resolve("Replacement disposition.", Timestamp(3)));
        Assert.Equal("Original disposition.", deviation.FinalDisposition);
        Assert.Equal(Timestamp(2), deviation.ResolvedAt);
    }

    [Fact]
    public void Resolve_BeforeApproval_ThrowsInvalidDeviationStateTransitionException()
    {
        var deviation = CreateDeviation();

        Assert.Throws<InvalidDeviationStateTransitionException>(
            () => deviation.Resolve("Premature disposition.", Timestamp(2)));
    }

    private static Deviation CreateApprovedDeviation()
    {
        var deviation = CreateDeviation();
        deviation.BeginReview("Quality review completed.");
        deviation.Approve();
        return deviation;
    }

    private static Deviation CreateDeviation(
        string code = "DEV-001",
        string batchNumber = "BATCH-001",
        BatchStepExecution? execution = null)
    {
        return new Deviation(
            new DeviationCode(code),
            new BatchNumber(batchNumber),
            execution,
            "Unexpected manufacturing condition.",
            new OperatorId("OP-001"),
            Timestamp(0));
    }

    private static BatchStepExecution CreateExecution(string batchNumber)
    {
        return new BatchStepExecution(
            new BatchStepExecutionId("EXEC-001"),
            new BatchNumber(batchNumber),
            new RecipeStepId("STEP-001"),
            1,
            false);
    }

    private static DateTimeOffset Timestamp(int hourOffset) =>
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero).AddHours(hourOffset);
}
