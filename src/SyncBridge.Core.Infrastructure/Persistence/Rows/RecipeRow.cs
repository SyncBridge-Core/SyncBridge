namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents a persisted recipe definition.
/// </summary>
public sealed class RecipeRow
{
    /// <summary>
    /// Gets or sets the recipe identity.
    /// </summary>
    public Guid RecipeId { get; set; }

    /// <summary>
    /// Gets or sets the unique recipe business code.
    /// </summary>
    public string RecipeCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipe name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the recipe version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the recipe is approved.
    /// </summary>
    public bool IsApproved { get; set; }
}
