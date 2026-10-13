using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational Batch record.
/// </summary>
public sealed class BatchRow
{
    /// <summary>Gets or sets the stable relational Batch identity.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Gets or sets the human-readable batch number.</summary>
    public string BatchNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the originating production-order identity.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the current Batch status.</summary>
    public BatchStatus Status { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the processing-start timestamp.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>Gets or sets the completion timestamp.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Gets or sets the optional final disposition.</summary>
    public string? FinalDisposition { get; set; }
}
