namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the current status of a production batch.
/// </summary>
public enum BatchStatus
{
    Created,
    Ready,
    InProgress,
    Exception,
    Completed
}
