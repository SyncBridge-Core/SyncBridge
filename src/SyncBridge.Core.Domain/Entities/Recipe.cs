using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production recipe.
/// </summary>
public sealed class Recipe
{
    public Recipe(
        RecipeCode recipeCode,
        ProductCode productCode,
        string version,
        string description,
        bool isActive)
    {
        RecipeCode = recipeCode;
        ProductCode = productCode;
        Version = version;
        Description = description;
        IsActive = isActive;
    }

    public RecipeCode RecipeCode { get; private set; }

    public ProductCode ProductCode { get; private set; }

    public string Version { get; private set; }

    public string Description { get; private set; }

    public bool IsActive { get; private set; }
}
