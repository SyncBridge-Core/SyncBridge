using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents invalid deviation evidence for a Batch lifecycle operation.
/// </summary>
public sealed class BatchDeviationException : DomainException
{
    /// <summary>
    /// Initializes a Batch deviation exception.
    /// </summary>
    /// <param name="batchNumber">The affected Batch identity.</param>
    /// <param name="reason">The reason the operation was rejected.</param>
    public BatchDeviationException(BatchNumber batchNumber, string reason)
        : base($"Batch '{batchNumber.Value}' has invalid deviation evidence. {reason}")
    {
        BatchNumber = batchNumber;
    }

    /// <summary>
    /// Gets the affected Batch identity.
    /// </summary>
    public BatchNumber BatchNumber { get; }
}
