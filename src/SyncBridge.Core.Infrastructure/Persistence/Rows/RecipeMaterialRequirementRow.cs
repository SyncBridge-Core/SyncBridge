namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents a persisted recipe material requirement.
/// </summary>
public sealed class RecipeMaterialRequirementRow
{
    /// <summary>
    /// Gets or sets the owning recipe identity.
    /// </summary>
    public Guid RecipeId { get; set; }

    /// <summary>
    /// Gets or sets the required material identity.
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Gets or sets the optional required quantity.
    /// </summary>
    public decimal? RequiredQuantity { get; set; }
}
