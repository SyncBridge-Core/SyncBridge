using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents an invalid transition between production order states.
/// </summary>
public sealed class InvalidProductionOrderStateTransitionException : DomainException
{
    /// <summary>
    /// Initializes a new invalid production order state transition exception.
    /// </summary>
    /// <param name="productionOrderNumber">The identifier of the affected production order.</param>
    /// <param name="currentStatus">The current production order status.</param>
    /// <param name="targetStatus">The attempted target production order status.</param>
    public InvalidProductionOrderStateTransitionException(
        ProductionOrderNumber productionOrderNumber,
        ProductionOrderStatus currentStatus,
        ProductionOrderStatus targetStatus)
        : base(
            $"Production order '{productionOrderNumber.Value}' cannot transition " +
            $"from '{currentStatus}' to '{targetStatus}'.")
    {
        ProductionOrderNumber = productionOrderNumber;
        CurrentStatus = currentStatus;
        TargetStatus = targetStatus;
    }

    /// <summary>
    /// Gets the identifier of the affected production order.
    /// </summary>
    public ProductionOrderNumber ProductionOrderNumber { get; }

    /// <summary>
    /// Gets the production order status at the time of the rejected transition.
    /// </summary>
    public ProductionOrderStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the rejected target production order status.
    /// </summary>
    public ProductionOrderStatus TargetStatus { get; }
}
