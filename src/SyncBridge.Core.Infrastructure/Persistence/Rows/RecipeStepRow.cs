namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents a persisted ordered recipe step.
/// </summary>
public sealed class RecipeStepRow
{
    /// <summary>
    /// Gets or sets the recipe-step identity.
    /// </summary>
    public Guid RecipeStepId { get; set; }

    /// <summary>
    /// Gets or sets the owning recipe identity.
    /// </summary>
    public Guid RecipeId { get; set; }

    /// <summary>
    /// Gets or sets the sequence position within the recipe.
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Gets or sets the manufacturing activity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator instructions.
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the step requires an electronic signature.
    /// </summary>
    public bool RequiresSignature { get; set; }
}
