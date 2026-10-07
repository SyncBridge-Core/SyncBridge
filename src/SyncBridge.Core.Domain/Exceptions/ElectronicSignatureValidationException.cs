using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents invalid electronic-signature evidence for a Batch operation.
/// </summary>
public sealed class ElectronicSignatureValidationException : DomainException
{
    /// <summary>
    /// Initializes an electronic-signature validation exception.
    /// </summary>
    /// <param name="batchNumber">The identity of the affected Batch.</param>
    /// <param name="reason">The reason the signature evidence was rejected.</param>
    public ElectronicSignatureValidationException(BatchNumber batchNumber, string reason)
        : base($"Batch '{batchNumber.Value}' does not have valid electronic-signature evidence. {reason}")
    {
        BatchNumber = batchNumber;
    }

    /// <summary>
    /// Gets the identity of the affected Batch.
    /// </summary>
    public BatchNumber BatchNumber { get; }
}
