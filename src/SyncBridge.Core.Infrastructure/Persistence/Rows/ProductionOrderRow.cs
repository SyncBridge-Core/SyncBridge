using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational ProductionOrder record.
/// </summary>
public sealed class ProductionOrderRow
{
    /// <summary>Gets or sets the production-order identity.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets or sets the human-readable order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the product code.</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the product display name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected Recipe identity.</summary>
    public Guid RecipeId { get; set; }

    /// <summary>Gets or sets the planned manufacturing quantity.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Gets or sets the production-order status.</summary>
    public ProductionOrderStatus Status { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
