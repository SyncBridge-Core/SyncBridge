using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents an electronic signature applied to a production batch.
/// </summary>
public sealed class ElectronicSignature
{
    public ElectronicSignature(
        BatchNumber batchNumber,
        OperatorId operatorId,
        ElectronicSignatureMeaning electronicSignatureMeaning,
        DateTimeOffset signedAt,
        string? comment)
    {
        BatchNumber = batchNumber;
        OperatorId = operatorId;
        ElectronicSignatureMeaning = electronicSignatureMeaning;
        SignedAt = signedAt;
        Comment = comment;
    }

    public BatchNumber BatchNumber { get; private set; }

    public OperatorId OperatorId { get; private set; }

    public ElectronicSignatureMeaning ElectronicSignatureMeaning { get; private set; }

    public DateTimeOffset SignedAt { get; private set; }

    public string? Comment { get; private set; }
}
