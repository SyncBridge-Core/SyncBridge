namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the runtime status of a Batch Recipe-step execution.
/// </summary>
public enum StepExecutionStatus
{
    Pending,
    InProgress,
    Completed
}
