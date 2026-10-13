using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents an invalid transition between production batch states.
/// </summary>
public sealed class InvalidBatchStateTransitionException : DomainException
{
    /// <summary>
    /// Initializes a new invalid batch state transition exception.
    /// </summary>
    /// <param name="batchNumber">The identifier of the affected batch.</param>
    /// <param name="currentStatus">The current batch status.</param>
    /// <param name="targetStatus">The attempted target batch status.</param>
    public InvalidBatchStateTransitionException(
        BatchNumber batchNumber,
        BatchStatus currentStatus,
        BatchStatus targetStatus)
        : base($"Batch '{batchNumber.Value}' cannot transition from '{currentStatus}' to '{targetStatus}'.")
    {
        BatchNumber = batchNumber;
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }

    /// <summary>
    /// Gets the identifier of the affected batch.
    /// </summary>
    public BatchNumber BatchNumber { get; }

    /// <summary>
    /// Gets the batch status at the time of the rejected transition.
    /// </summary>
    public BatchStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the rejected target batch status.
    /// </summary>
    public BatchStatus TargetStatus { get; }
}
