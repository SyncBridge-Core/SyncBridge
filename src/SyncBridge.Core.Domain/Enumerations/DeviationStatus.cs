namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the current status of a deviation.
/// </summary>
public enum DeviationStatus
{
    Open,
    UnderReview,
    Approved,
    Rejected,
    Closed
}
