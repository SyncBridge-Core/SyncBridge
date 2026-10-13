using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational BatchStepExecution record.
/// </summary>
public sealed class BatchStepExecutionRow
{
    /// <summary>Gets or sets the step-execution identity.</summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>Gets or sets the owning Batch identity.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Gets or sets the source RecipeStep identity.</summary>
    public Guid RecipeStepId { get; set; }

    /// <summary>Gets or sets the execution sequence number.</summary>
    public int StepNumber { get; set; }

    /// <summary>Gets or sets the execution status.</summary>
    public StepExecutionStatus Status { get; set; }

    /// <summary>Gets or sets the start timestamp.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>Gets or sets the completion timestamp.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Gets or sets the optional completing user identity.</summary>
    public string? CompletedByUserId { get; set; }

    /// <summary>Gets or sets optional operator-entered execution evidence.</summary>
    public string? RecordedData { get; set; }
}
