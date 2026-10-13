using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents a failure to satisfy the prerequisite-verification gate for a Batch.
/// </summary>
public sealed class BatchPrerequisiteVerificationException : DomainException
{
    /// <summary>
    /// Initializes a Batch prerequisite-verification exception.
    /// </summary>
    /// <param name="batchNumber">The identity of the affected Batch.</param>
    /// <param name="reason">The reason prerequisite validation failed.</param>
    public BatchPrerequisiteVerificationException(BatchNumber batchNumber, string reason)
        : base($"Batch '{batchNumber.Value}' cannot start processing. {reason}")
    {
        BatchNumber = batchNumber;
    }

    /// <summary>
    /// Gets the identity of the affected Batch.
    /// </summary>
    public BatchNumber BatchNumber { get; }
}
