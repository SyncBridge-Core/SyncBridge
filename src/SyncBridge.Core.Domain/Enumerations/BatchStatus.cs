namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the current status of a production batch.
/// </summary>
public enum BatchStatus
{
    Created,
    Prepared,
    MaterialVerified,
    EquipmentVerified,
    InProcess,
    QualityReview,
    Completed,
    Cancelled
}
