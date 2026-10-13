namespace SyncBridge.Core.Domain.ValueObjects;

/// <summary>
/// Represents the immutable identity assigned to an audit event.
/// </summary>
/// <param name="Value">The audit-event identity value.</param>
public sealed record AuditEventId(string Value);
