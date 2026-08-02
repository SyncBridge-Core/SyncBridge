using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production order.
/// </summary>
public sealed class ProductionOrder
{
    public ProductionOrder(
        ProductionOrderNumber productionOrderNumber,
        ProductCode productCode,
        string description,
        ProductionOrderStatus status,
        DateTimeOffset plannedStart,
        DateTimeOffset plannedEnd)
    {
        ProductionOrderNumber = productionOrderNumber;
        ProductCode = productCode;
        Description = description;
        ProductionOrderStatus = status;
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
    }

    public ProductionOrderNumber ProductionOrderNumber { get; private set; }

    public ProductCode ProductCode { get; private set; }

    public string Description { get; private set; }

    public ProductionOrderStatus ProductionOrderStatus { get; private set; }

    public DateTimeOffset PlannedStart { get; private set; }

    public DateTimeOffset PlannedEnd { get; private set; }
}
