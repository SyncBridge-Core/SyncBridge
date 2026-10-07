namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the type of an audit event.
/// </summary>
public enum AuditEventType
{
    /// <summary>The Batch was created.</summary>
    BatchCreated,
    /// <summary>The Batch was marked ready.</summary>
    BatchMarkedReady,
    /// <summary>The Batch began processing.</summary>
    BatchProcessingStarted,
    /// <summary>A material verification was recorded.</summary>
    MaterialVerified,
    /// <summary>An equipment verification was recorded.</summary>
    EquipmentVerified,
    /// <summary>A Recipe-step execution was started.</summary>
    StepStarted,
    /// <summary>A Recipe-step execution was completed.</summary>
    StepCompleted,
    /// <summary>An electronic signature was applied.</summary>
    ElectronicSignatureApplied,
    /// <summary>A deviation was opened.</summary>
    DeviationOpened,
    /// <summary>A deviation entered review.</summary>
    DeviationReviewStarted,
    /// <summary>A deviation was approved.</summary>
    DeviationApproved,
    /// <summary>A deviation was rejected.</summary>
    DeviationRejected,
    /// <summary>A deviation was resolved and closed.</summary>
    DeviationResolved,
    /// <summary>The Batch entered Exception.</summary>
    BatchExceptionEntered,
    /// <summary>The Batch resumed processing.</summary>
    BatchProcessingResumed,
    /// <summary>The Batch was completed.</summary>
    BatchCompleted
}
