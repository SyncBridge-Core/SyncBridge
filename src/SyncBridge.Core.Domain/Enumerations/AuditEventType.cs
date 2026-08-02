namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the type of an audit event.
/// </summary>
public enum AuditEventType
{
    Created,
    Updated,
    Verified,
    Approved,
    Released,
    DeviationLogged,
    Completed,
    Cancelled
}
