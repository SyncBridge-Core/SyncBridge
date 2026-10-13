using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the append-only relational AuditEvent record.
/// </summary>
public sealed class AuditEventRow
{
    /// <summary>Gets or sets the audit-event identity.</summary>
    public Guid AuditEventId { get; set; }

    /// <summary>Gets or sets the optional associated Batch identity.</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Gets or sets the responsible user identity.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the action performed.</summary>
    public AuditEventType Action { get; set; }

    /// <summary>Gets or sets the affected entity type.</summary>
    public string AffectedEntity { get; set; } = string.Empty;

    /// <summary>Gets or sets the affected entity display identity.</summary>
    public string AffectedEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the event timestamp.</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Gets or sets the optional resulting state.</summary>
    public string? ResultingState { get; set; }

    /// <summary>Gets or sets optional event details.</summary>
    public string? Details { get; set; }
}
