using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents immutable evidence of a significant domain action.
/// </summary>
public sealed class AuditEvent
{
    /// <summary>
    /// Initializes immutable audit evidence.
    /// </summary>
    /// <param name="auditEventId">The audit-event identity.</param>
    /// <param name="batchNumber">The optional associated Batch identity.</param>
    /// <param name="operatorId">The responsible operator identity.</param>
    /// <param name="action">The action performed.</param>
    /// <param name="affectedEntity">The affected entity type.</param>
    /// <param name="affectedEntityId">The affected entity identity.</param>
    /// <param name="occurredAt">The action timestamp.</param>
    /// <param name="resultingState">The optional resulting state or context.</param>
    /// <param name="details">Optional concise action details.</param>
    public AuditEvent(
        AuditEventId auditEventId,
        BatchNumber? batchNumber,
        OperatorId operatorId,
        AuditEventType action,
        string affectedEntity,
        string affectedEntityId,
        DateTimeOffset occurredAt,
        string? resultingState = null,
        string? details = null)
    {
        ArgumentNullException.ThrowIfNull(auditEventId);
        ArgumentNullException.ThrowIfNull(operatorId);

        if (string.IsNullOrWhiteSpace(auditEventId.Value))
        {
            throw new ArgumentException("Audit-event identity is required.", nameof(auditEventId));
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action));
        }

        if (string.IsNullOrWhiteSpace(affectedEntity))
        {
            throw new ArgumentException("Affected entity is required.", nameof(affectedEntity));
        }

        if (string.IsNullOrWhiteSpace(affectedEntityId))
        {
            throw new ArgumentException("Affected entity identity is required.", nameof(affectedEntityId));
        }

        AuditEventId = auditEventId;
        BatchNumber = batchNumber;
        OperatorId = operatorId;
        Action = action;
        AffectedEntity = affectedEntity;
        AffectedEntityId = affectedEntityId;
        OccurredAt = occurredAt;
        ResultingState = resultingState;
        Details = details;
    }

    /// <summary>Gets the audit-event identity.</summary>
    public AuditEventId AuditEventId { get; }

    /// <summary>Gets the optional associated Batch identity.</summary>
    public BatchNumber? BatchNumber { get; }

    /// <summary>Gets the responsible operator identity.</summary>
    public OperatorId OperatorId { get; }

    /// <summary>Gets the action performed.</summary>
    public AuditEventType Action { get; }

    /// <summary>Gets the affected entity type.</summary>
    public string AffectedEntity { get; }

    /// <summary>Gets the affected entity identity.</summary>
    public string AffectedEntityId { get; }

    /// <summary>Gets the action timestamp.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Gets the optional resulting state or context.</summary>
    public string? ResultingState { get; }

    /// <summary>Gets optional concise action details.</summary>
    public string? Details { get; }

    internal static AuditEvent Create(
        BatchNumber? batchNumber,
        OperatorId operatorId,
        AuditEventType action,
        string affectedEntity,
        string affectedEntityId,
        DateTimeOffset occurredAt,
        string? resultingState = null,
        string? details = null)
    {
        return new AuditEvent(
            new AuditEventId(Guid.NewGuid().ToString("D")),
            batchNumber,
            operatorId,
            action,
            affectedEntity,
            affectedEntityId,
            occurredAt,
            resultingState,
            details);
    }
}
