namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents a persisted recipe equipment requirement.
/// </summary>
public sealed class RecipeEquipmentRequirementRow
{
    /// <summary>
    /// Gets or sets the owning recipe identity.
    /// </summary>
    public Guid RecipeId { get; set; }

    /// <summary>
    /// Gets or sets the required equipment identity.
    /// </summary>
    public Guid EquipmentId { get; set; }
}
