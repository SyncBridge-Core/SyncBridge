using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production order.
/// </summary>
public sealed class ProductionOrder
{
    /// <summary>
    /// Initializes a new production order in the planned state.
    /// </summary>
    /// <param name="productionOrderNumber">The production order identifier.</param>
    /// <param name="productCode">The product code.</param>
    /// <param name="description">The production order description.</param>
    /// <param name="plannedStart">The planned start date and time.</param>
    /// <param name="plannedEnd">The planned end date and time.</param>
    public ProductionOrder(
        ProductionOrderNumber productionOrderNumber,
        ProductCode productCode,
        string description,
        DateTimeOffset plannedStart,
        DateTimeOffset plannedEnd)
    {
        ArgumentNullException.ThrowIfNull(productionOrderNumber);
        ArgumentNullException.ThrowIfNull(productCode);

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (plannedStart >= plannedEnd)
        {
            throw new ArgumentException(
                "Planned start must be earlier than planned end.",
                nameof(plannedStart));
        }

        ProductionOrderNumber = productionOrderNumber;
        ProductCode = productCode;
        Description = description;
        ProductionOrderStatus = ProductionOrderStatus.Planned;
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
    }

    /// <summary>
    /// Gets the production order identifier.
    /// </summary>
    public ProductionOrderNumber ProductionOrderNumber { get; private set; }

    /// <summary>
    /// Gets the product code.
    /// </summary>
    public ProductCode ProductCode { get; private set; }

    /// <summary>
    /// Gets the production order description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the current production order status.
    /// </summary>
    public ProductionOrderStatus ProductionOrderStatus { get; private set; }

    /// <summary>
    /// Gets the planned start date and time.
    /// </summary>
    public DateTimeOffset PlannedStart { get; private set; }

    /// <summary>
    /// Gets the planned end date and time.
    /// </summary>
    public DateTimeOffset PlannedEnd { get; private set; }

    /// <summary>
    /// Releases the planned production order.
    /// </summary>
    public void Release()
    {
        TransitionTo(ProductionOrderStatus.Released, ProductionOrderStatus.Planned);
    }

    /// <summary>
    /// Starts processing the released production order.
    /// </summary>
    public void StartProcessing()
    {
        TransitionTo(ProductionOrderStatus.InProcess, ProductionOrderStatus.Released);
    }

    /// <summary>
    /// Completes the in-process production order.
    /// </summary>
    public void Complete()
    {
        TransitionTo(ProductionOrderStatus.Completed, ProductionOrderStatus.InProcess);
    }

    /// <summary>
    /// Cancels a production order that has not reached a terminal state.
    /// </summary>
    public void Cancel()
    {
        if (ProductionOrderStatus is ProductionOrderStatus.Completed or ProductionOrderStatus.Cancelled)
        {
            throw new InvalidProductionOrderStateTransitionException(
                ProductionOrderNumber,
                ProductionOrderStatus,
                ProductionOrderStatus.Cancelled);
        }

        ProductionOrderStatus = ProductionOrderStatus.Cancelled;
    }

    private void TransitionTo(
        ProductionOrderStatus targetStatus,
        ProductionOrderStatus requiredStatus)
    {
        if (ProductionOrderStatus != requiredStatus)
        {
            throw new InvalidProductionOrderStateTransitionException(
                ProductionOrderNumber,
                ProductionOrderStatus,
                targetStatus);
        }

        ProductionOrderStatus = targetStatus;
    }
}
