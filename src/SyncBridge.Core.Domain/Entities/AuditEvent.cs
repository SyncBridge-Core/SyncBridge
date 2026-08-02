using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents an auditable event associated with a production batch.
/// </summary>
public sealed class AuditEvent
{
    public AuditEvent(
        BatchNumber batchNumber,
        DateTimeOffset occurredAt,
        AuditEventType auditEventType,
        string description,
        OperatorId? operatorId)
    {
        BatchNumber = batchNumber;
        OccurredAt = occurredAt;
        AuditEventType = auditEventType;
        Description = description;
        OperatorId = operatorId;
    }

    public BatchNumber BatchNumber { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public AuditEventType AuditEventType { get; private set; }

    public string Description { get; private set; }

    public OperatorId? OperatorId { get; private set; }
}
