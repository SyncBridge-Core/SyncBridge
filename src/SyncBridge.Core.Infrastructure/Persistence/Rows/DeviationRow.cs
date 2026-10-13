using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational Deviation record.
/// </summary>
public sealed class DeviationRow
{
    /// <summary>Gets or sets the stable relational Deviation identity.</summary>
    public Guid DeviationId { get; set; }

    /// <summary>Gets or sets the human-readable deviation code.</summary>
    public string DeviationCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the originating Batch identity.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Gets or sets the optional originating step-execution identity.</summary>
    public Guid? StepExecutionId { get; set; }

    /// <summary>Gets or sets the abnormal-condition description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the current deviation status.</summary>
    public DeviationStatus Status { get; set; }

    /// <summary>Gets or sets the reporting user identity.</summary>
    public string ReportedByUserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the opening timestamp.</summary>
    public DateTimeOffset OpenedAt { get; set; }

    /// <summary>Gets or sets optional quality-review comments.</summary>
    public string? ReviewComments { get; set; }

    /// <summary>Gets or sets the optional final disposition.</summary>
    public string? FinalDisposition { get; set; }

    /// <summary>Gets or sets the optional resolution timestamp.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }
}
