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
    /// <param name="productionOrderId">The production-order identity.</param>
    /// <param name="productionOrderNumber">The production order identifier.</param>
    /// <param name="productCode">The product code.</param>
    /// <param name="productName">The product name used for display and reporting.</param>
    /// <param name="recipeId">The selected approved Recipe identity.</param>
    /// <param name="quantity">The planned manufacturing quantity.</param>
    /// <param name="createdAt">The production-order creation timestamp.</param>
    public ProductionOrder(
        ProductionOrderId productionOrderId,
        ProductionOrderNumber productionOrderNumber,
        ProductCode productCode,
        string productName,
        RecipeId recipeId,
        decimal quantity,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(productionOrderId);
        ArgumentNullException.ThrowIfNull(productionOrderNumber);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(recipeId);

        if (productionOrderId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Production-order identity is required.",
                nameof(productionOrderId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name is required.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Production-order quantity must be greater than zero.");
        }

        ProductionOrderId = productionOrderId;
        ProductionOrderNumber = productionOrderNumber;
        ProductCode = productCode;
        ProductName = productName;
        RecipeId = recipeId;
        Quantity = quantity;
        ProductionOrderStatus = ProductionOrderStatus.Planned;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the production-order identity.
    /// </summary>
    public ProductionOrderId ProductionOrderId { get; }

    /// <summary>
    /// Gets the production order identifier.
    /// </summary>
    public ProductionOrderNumber ProductionOrderNumber { get; private set; }

    /// <summary>
    /// Gets the product code.
    /// </summary>
    public ProductCode ProductCode { get; private set; }

    /// <summary>
    /// Gets the product name used for display and reporting.
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// Gets the selected approved Recipe identity.
    /// </summary>
    public RecipeId RecipeId { get; }

    /// <summary>
    /// Gets the planned manufacturing quantity.
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// Gets the current production order status.
    /// </summary>
    public ProductionOrderStatus ProductionOrderStatus { get; private set; }

    /// <summary>
    /// Gets the production-order creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

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
