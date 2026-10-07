using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents an invalid Batch Recipe-step execution operation.
/// </summary>
public sealed class InvalidBatchStepExecutionException : DomainException
{
    /// <summary>
    /// Initializes an invalid Batch step-execution exception.
    /// </summary>
    /// <param name="batchNumber">The identity of the affected Batch.</param>
    /// <param name="reason">The reason the operation was rejected.</param>
    public InvalidBatchStepExecutionException(BatchNumber batchNumber, string reason)
        : base($"Batch '{batchNumber.Value}' cannot execute the Recipe step. {reason}")
    {
        BatchNumber = batchNumber;
    }

    /// <summary>
    /// Gets the identity of the affected Batch.
    /// </summary>
    public BatchNumber BatchNumber { get; }
}
