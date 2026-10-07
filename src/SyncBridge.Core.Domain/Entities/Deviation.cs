using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a documented abnormal manufacturing condition for a Batch.
/// </summary>
public sealed class Deviation
{
    private readonly List<AuditEvent> _auditEvents = [];

    /// <summary>
    /// Initializes an open Batch-level deviation.
    /// </summary>
    /// <param name="deviationCode">The deviation identity.</param>
    /// <param name="batchNumber">The originating Batch identity.</param>
    /// <param name="description">The abnormal-condition description.</param>
    /// <param name="reportedBy">The reporting operator identity.</param>
    /// <param name="openedAt">The timestamp at which the deviation was opened.</param>
    public Deviation(
        DeviationCode deviationCode,
        BatchNumber batchNumber,
        string description,
        OperatorId reportedBy,
        DateTimeOffset openedAt)
        : this(deviationCode, batchNumber, null, description, reportedBy, openedAt)
    {
    }

    /// <summary>
    /// Initializes an open deviation for a specific Batch step execution.
    /// </summary>
    /// <param name="deviationCode">The deviation identity.</param>
    /// <param name="batchNumber">The originating Batch identity.</param>
    /// <param name="originatingStepExecution">The optional originating step execution.</param>
    /// <param name="description">The abnormal-condition description.</param>
    /// <param name="reportedBy">The reporting operator identity.</param>
    /// <param name="openedAt">The timestamp at which the deviation was opened.</param>
    public Deviation(
        DeviationCode deviationCode,
        BatchNumber batchNumber,
        BatchStepExecution? originatingStepExecution,
        string description,
        OperatorId reportedBy,
        DateTimeOffset openedAt)
    {
        ArgumentNullException.ThrowIfNull(deviationCode);
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(reportedBy);

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Deviation description is required.", nameof(description));
        }

        if (originatingStepExecution is not null &&
            originatingStepExecution.BatchNumber != batchNumber)
        {
            throw new DomainException(
                "The originating step execution must belong to the deviation's Batch.");
        }

        DeviationCode = deviationCode;
        BatchNumber = batchNumber;
        BatchStepExecutionId = originatingStepExecution?.BatchStepExecutionId;
        Description = description;
        ReportedBy = reportedBy;
        OpenedAt = openedAt;
        DeviationStatus = DeviationStatus.Open;
        RecordAudit(
            reportedBy,
            AuditEventType.DeviationOpened,
            openedAt,
            DeviationStatus.Open,
            description);
    }

    /// <summary>
    /// Gets the deviation identity.
    /// </summary>
    public DeviationCode DeviationCode { get; }

    /// <summary>
    /// Gets the permanently associated originating Batch identity.
    /// </summary>
    public BatchNumber BatchNumber { get; }

    /// <summary>
    /// Gets the optional originating step-execution identity.
    /// </summary>
    public BatchStepExecutionId? BatchStepExecutionId { get; }

    /// <summary>
    /// Gets the abnormal-condition description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the reporting operator identity.
    /// </summary>
    public OperatorId ReportedBy { get; }

    /// <summary>
    /// Gets the timestamp at which the deviation was opened.
    /// </summary>
    public DateTimeOffset OpenedAt { get; }

    /// <summary>
    /// Gets the current deviation status.
    /// </summary>
    public DeviationStatus DeviationStatus { get; private set; }

    /// <summary>
    /// Gets the recorded review comments, when available.
    /// </summary>
    public string? ReviewComments { get; private set; }

    /// <summary>
    /// Gets the final disposition, when available.
    /// </summary>
    public string? FinalDisposition { get; private set; }

    /// <summary>
    /// Gets the resolution timestamp, when available.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>
    /// Gets whether the deviation blocks normal Batch processing.
    /// </summary>
    public bool IsBlocking => DeviationStatus != DeviationStatus.Closed;

    /// <summary>
    /// Gets the immutable audit evidence produced by this deviation lifecycle.
    /// </summary>
    public IReadOnlyList<AuditEvent> AuditEvents => _auditEvents.ToArray();

    /// <summary>
    /// Moves the open deviation into review and records review comments.
    /// </summary>
    /// <param name="reviewComments">The review comments.</param>
    /// <param name="reviewedBy">The responsible reviewing operator.</param>
    /// <param name="reviewedAt">The review timestamp.</param>
    public void BeginReview(
        string reviewComments,
        OperatorId reviewedBy,
        DateTimeOffset reviewedAt)
    {
        EnsureStatus(DeviationStatus.Open, "begin review");
        ArgumentNullException.ThrowIfNull(reviewedBy);

        if (string.IsNullOrWhiteSpace(reviewComments))
        {
            throw new ArgumentException("Review comments are required.", nameof(reviewComments));
        }

        ReviewComments = reviewComments;
        DeviationStatus = DeviationStatus.UnderReview;
        RecordAudit(
            reviewedBy,
            AuditEventType.DeviationReviewStarted,
            reviewedAt,
            DeviationStatus.UnderReview,
            reviewComments);
    }

    /// <summary>
    /// Records approval of a deviation under review.
    /// </summary>
    /// <param name="approvedBy">The responsible approving operator.</param>
    /// <param name="approvedAt">The approval timestamp.</param>
    public void Approve(OperatorId approvedBy, DateTimeOffset approvedAt)
    {
        EnsureStatus(DeviationStatus.UnderReview, "be approved");
        ArgumentNullException.ThrowIfNull(approvedBy);
        DeviationStatus = DeviationStatus.Approved;
        RecordAudit(
            approvedBy,
            AuditEventType.DeviationApproved,
            approvedAt,
            DeviationStatus.Approved);
    }

    /// <summary>
    /// Records rejection of a deviation under review.
    /// </summary>
    /// <param name="rejectedBy">The responsible rejecting operator.</param>
    /// <param name="rejectedAt">The rejection timestamp.</param>
    public void Reject(OperatorId rejectedBy, DateTimeOffset rejectedAt)
    {
        EnsureStatus(DeviationStatus.UnderReview, "be rejected");
        ArgumentNullException.ThrowIfNull(rejectedBy);
        DeviationStatus = DeviationStatus.Rejected;
        RecordAudit(
            rejectedBy,
            AuditEventType.DeviationRejected,
            rejectedAt,
            DeviationStatus.Rejected);
    }

    /// <summary>
    /// Resolves an approved deviation with its final disposition.
    /// </summary>
    /// <param name="finalDisposition">The final disposition.</param>
    /// <param name="resolvedAt">The resolution timestamp.</param>
    /// <param name="resolvedBy">The responsible resolving operator.</param>
    public void Resolve(
        string finalDisposition,
        DateTimeOffset resolvedAt,
        OperatorId resolvedBy)
    {
        EnsureStatus(DeviationStatus.Approved, "be resolved");
        ArgumentNullException.ThrowIfNull(resolvedBy);

        if (string.IsNullOrWhiteSpace(finalDisposition))
        {
            throw new ArgumentException("Final disposition is required.", nameof(finalDisposition));
        }

        FinalDisposition = finalDisposition;
        ResolvedAt = resolvedAt;
        DeviationStatus = DeviationStatus.Closed;
        RecordAudit(
            resolvedBy,
            AuditEventType.DeviationResolved,
            resolvedAt,
            DeviationStatus.Closed,
            finalDisposition);
    }

    private void EnsureStatus(DeviationStatus requiredStatus, string operation)
    {
        if (DeviationStatus != requiredStatus)
        {
            throw new InvalidDeviationStateTransitionException(
                DeviationCode,
                DeviationStatus,
                operation);
        }
    }

    private void RecordAudit(
        OperatorId operatorId,
        AuditEventType action,
        DateTimeOffset occurredAt,
        DeviationStatus resultingStatus,
        string? details = null)
    {
        _auditEvents.Add(AuditEvent.Create(
            BatchNumber,
            operatorId,
            action,
            nameof(Deviation),
            DeviationCode.Value,
            occurredAt,
            resultingStatus.ToString(),
            details));
    }
}
