namespace SyncBridge.Core.Domain.Enumerations;

/// <summary>
/// Represents the current status of a production order.
/// </summary>
public enum ProductionOrderStatus
{
    Planned,
    Released,
    InProcess,
    Completed,
    Cancelled
}
