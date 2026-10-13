using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents an ordered step in a manufacturing recipe definition.
/// </summary>
public sealed class RecipeStep
{
    /// <summary>
    /// Initializes a recipe step definition.
    /// </summary>
    /// <param name="recipeStepId">The recipe-step identity.</param>
    /// <param name="recipeId">The identity of the owning recipe.</param>
    /// <param name="stepNumber">The prescribed execution order.</param>
    /// <param name="name">The step name.</param>
    /// <param name="instructions">The step instructions.</param>
    /// <param name="requiresSignature">Whether the step definition requires an electronic signature.</param>
    public RecipeStep(
        RecipeStepId recipeStepId,
        RecipeId recipeId,
        int stepNumber,
        string name,
        string instructions,
        bool requiresSignature)
    {
        ArgumentNullException.ThrowIfNull(recipeStepId);
        ArgumentNullException.ThrowIfNull(recipeId);

        if (stepNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepNumber),
                "Step number must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Recipe step name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(instructions))
        {
            throw new ArgumentException(
                "Recipe step instructions are required.",
                nameof(instructions));
        }

        RecipeStepId = recipeStepId;
        RecipeId = recipeId;
        StepNumber = stepNumber;
        Name = name;
        Instructions = instructions;
        RequiresSignature = requiresSignature;
    }

    /// <summary>
    /// Gets the recipe-step identity.
    /// </summary>
    public RecipeStepId RecipeStepId { get; }

    /// <summary>
    /// Gets the identity of the owning recipe.
    /// </summary>
    public RecipeId RecipeId { get; }

    /// <summary>
    /// Gets the prescribed execution order.
    /// </summary>
    public int StepNumber { get; }

    /// <summary>
    /// Gets the step name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the step instructions.
    /// </summary>
    public string Instructions { get; }

    /// <summary>
    /// Gets a value indicating whether the step definition requires an electronic signature.
    /// </summary>
    public bool RequiresSignature { get; }
}
