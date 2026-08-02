using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a deviation recorded against a production batch.
/// </summary>
public sealed class Deviation
{
    public Deviation(
        DeviationCode deviationCode,
        BatchNumber batchNumber,
        DeviationStatus deviationStatus,
        string description,
        DateTimeOffset createdAt)
    {
        DeviationCode = deviationCode;
        BatchNumber = batchNumber;
        DeviationStatus = deviationStatus;
        Description = description;
        CreatedAt = createdAt;
    }

    public DeviationCode DeviationCode { get; private set; }

    public BatchNumber BatchNumber { get; private set; }

    public DeviationStatus DeviationStatus { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
