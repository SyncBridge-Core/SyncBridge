using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a material required by a manufacturing recipe.
/// </summary>
public sealed class RecipeMaterialRequirement
{
    /// <summary>
    /// Initializes a recipe material requirement.
    /// </summary>
    /// <param name="recipeId">The identity of the owning recipe.</param>
    /// <param name="materialCode">The required material identity.</param>
    /// <param name="requiredQuantity">The optional required material quantity.</param>
    public RecipeMaterialRequirement(
        RecipeId recipeId,
        MaterialCode materialCode,
        decimal? requiredQuantity)
    {
        ArgumentNullException.ThrowIfNull(recipeId);
        ArgumentNullException.ThrowIfNull(materialCode);

        if (requiredQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredQuantity),
                "Required quantity must be greater than zero when supplied.");
        }

        RecipeId = recipeId;
        MaterialCode = materialCode;
        RequiredQuantity = requiredQuantity;
    }

    /// <summary>
    /// Gets the identity of the owning recipe.
    /// </summary>
    public RecipeId RecipeId { get; }

    /// <summary>
    /// Gets the required material identity.
    /// </summary>
    public MaterialCode MaterialCode { get; }

    /// <summary>
    /// Gets the optional required material quantity.
    /// </summary>
    public decimal? RequiredQuantity { get; }
}
