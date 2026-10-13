using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents equipment required by a manufacturing recipe.
/// </summary>
public sealed class RecipeEquipmentRequirement
{
    /// <summary>
    /// Initializes a recipe equipment requirement.
    /// </summary>
    /// <param name="recipeId">The identity of the owning recipe.</param>
    /// <param name="equipmentCode">The required equipment identity.</param>
    public RecipeEquipmentRequirement(RecipeId recipeId, EquipmentCode equipmentCode)
    {
        ArgumentNullException.ThrowIfNull(recipeId);
        ArgumentNullException.ThrowIfNull(equipmentCode);

        RecipeId = recipeId;
        EquipmentCode = equipmentCode;
    }

    /// <summary>
    /// Gets the identity of the owning recipe.
    /// </summary>
    public RecipeId RecipeId { get; }

    /// <summary>
    /// Gets the required equipment identity.
    /// </summary>
    public EquipmentCode EquipmentCode { get; }
}
