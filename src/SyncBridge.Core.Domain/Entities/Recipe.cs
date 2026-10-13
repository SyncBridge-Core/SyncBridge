using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production recipe.
/// </summary>
public sealed class Recipe
{
    private readonly List<RecipeStep> _steps = [];
    private readonly List<RecipeMaterialRequirement> _materialRequirements = [];
    private readonly List<RecipeEquipmentRequirement> _equipmentRequirements = [];

    /// <summary>
    /// Initializes a manufacturing recipe definition.
    /// </summary>
    /// <param name="recipeId">The recipe identity.</param>
    /// <param name="recipeCode">The recipe code.</param>
    /// <param name="name">The recipe name.</param>
    /// <param name="version">The recipe version.</param>
    /// <param name="isApproved">Whether the recipe is approved.</param>
    public Recipe(
        RecipeId recipeId,
        RecipeCode recipeCode,
        string name,
        string version,
        bool isApproved)
    {
        ArgumentNullException.ThrowIfNull(recipeId);
        ArgumentNullException.ThrowIfNull(recipeCode);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Recipe name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Recipe version is required.", nameof(version));
        }

        RecipeId = recipeId;
        RecipeCode = recipeCode;
        Name = name;
        Version = version;
        IsApproved = isApproved;
    }

    /// <summary>
    /// Gets the recipe identity.
    /// </summary>
    public RecipeId RecipeId { get; }

    /// <summary>
    /// Gets the recipe code.
    /// </summary>
    public RecipeCode RecipeCode { get; private set; }

    /// <summary>
    /// Gets the recipe name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the recipe version.
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the recipe is approved.
    /// </summary>
    public bool IsApproved { get; private set; }

    /// <summary>
    /// Gets the recipe steps in prescribed execution order.
    /// </summary>
    public IReadOnlyList<RecipeStep> Steps => _steps
        .OrderBy(step => step.StepNumber)
        .ToArray();

    /// <summary>
    /// Gets the material requirements for the recipe.
    /// </summary>
    public IReadOnlyList<RecipeMaterialRequirement> MaterialRequirements =>
        _materialRequirements.ToArray();

    /// <summary>
    /// Gets the equipment requirements for the recipe.
    /// </summary>
    public IReadOnlyList<RecipeEquipmentRequirement> EquipmentRequirements =>
        _equipmentRequirements.ToArray();

    /// <summary>
    /// Adds a step to the recipe definition.
    /// </summary>
    /// <param name="step">The step to add.</param>
    public void AddStep(RecipeStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        EnsureOwns(step.RecipeId, "Recipe step");

        if (_steps.Any(existingStep => existingStep.StepNumber == step.StepNumber))
        {
            throw new DomainException(
                $"Recipe '{RecipeCode.Value}' already contains step number {step.StepNumber}.");
        }

        _steps.Add(step);
    }

    /// <summary>
    /// Adds a material requirement to the recipe definition.
    /// </summary>
    /// <param name="requirement">The material requirement to add.</param>
    public void AddMaterialRequirement(RecipeMaterialRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        EnsureOwns(requirement.RecipeId, "Material requirement");
        _materialRequirements.Add(requirement);
    }

    /// <summary>
    /// Adds an equipment requirement to the recipe definition.
    /// </summary>
    /// <param name="requirement">The equipment requirement to add.</param>
    public void AddEquipmentRequirement(RecipeEquipmentRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        EnsureOwns(requirement.RecipeId, "Equipment requirement");
        _equipmentRequirements.Add(requirement);
    }

    private void EnsureOwns(RecipeId recipeId, string definitionType)
    {
        if (recipeId != RecipeId)
        {
            throw new DomainException($"{definitionType} must belong to the recipe.");
        }
    }
}
