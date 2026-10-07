using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class RecipeTests
{
    [Fact]
    public void Constructor_PreservesRequiredValuesAndApprovedStatus()
    {
        var recipeId = new RecipeId("RECIPE-ID-001");
        var recipeCode = new RecipeCode("RECIPE-001");

        var recipe = new Recipe(recipeId, recipeCode, "Tablet Blend", "1.0", true);

        Assert.Equal(recipeId, recipe.RecipeId);
        Assert.Equal(recipeCode, recipe.RecipeCode);
        Assert.Equal("Tablet Blend", recipe.Name);
        Assert.Equal("1.0", recipe.Version);
        Assert.True(recipe.IsApproved);
    }

    [Fact]
    public void Constructor_WhenNotApproved_PreservesApprovalStatus()
    {
        var recipe = CreateRecipe(isApproved: false);

        Assert.False(recipe.IsApproved);
    }

    [Fact]
    public void Constructor_WithNullRecipeId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Recipe(null!, new RecipeCode("RECIPE-001"), "Tablet Blend", "1.0", true));
    }

    [Fact]
    public void Constructor_WithNullRecipeCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Recipe(new RecipeId("RECIPE-ID-001"), null!, "Tablet Blend", "1.0", true));
    }

    [Fact]
    public void Constructor_WithBlankName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Recipe(
                new RecipeId("RECIPE-ID-001"),
                new RecipeCode("RECIPE-001"),
                " ",
                "1.0",
                true));
    }

    [Fact]
    public void Constructor_WithBlankVersion_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Recipe(
                new RecipeId("RECIPE-ID-001"),
                new RecipeCode("RECIPE-001"),
                "Tablet Blend",
                " ",
                true));
    }

    [Fact]
    public void Steps_AreReturnedInStepNumberOrder()
    {
        var recipe = CreateRecipe();
        recipe.AddStep(CreateStep(recipe.RecipeId, "STEP-003", 3));
        recipe.AddStep(CreateStep(recipe.RecipeId, "STEP-001", 1));
        recipe.AddStep(CreateStep(recipe.RecipeId, "STEP-002", 2));

        var stepNumbers = recipe.Steps.Select(step => step.StepNumber);

        Assert.Equal(new[] { 1, 2, 3 }, stepNumbers);
    }

    [Fact]
    public void AddStep_WithDuplicateStepNumber_ThrowsDomainException()
    {
        var recipe = CreateRecipe();
        recipe.AddStep(CreateStep(recipe.RecipeId, "STEP-001", 1));

        Assert.Throws<DomainException>(
            () => recipe.AddStep(CreateStep(recipe.RecipeId, "STEP-002", 1)));
        Assert.Single(recipe.Steps);
    }

    [Fact]
    public void AddStep_FromDifferentRecipe_ThrowsDomainException()
    {
        var recipe = CreateRecipe();
        var foreignStep = CreateStep(new RecipeId("RECIPE-ID-002"), "STEP-001", 1);

        Assert.Throws<DomainException>(() => recipe.AddStep(foreignStep));
        Assert.Empty(recipe.Steps);
    }

    [Fact]
    public void AddMaterialRequirement_AddsOwnedRequirement()
    {
        var recipe = CreateRecipe();
        var requirement = new RecipeMaterialRequirement(
            recipe.RecipeId,
            new MaterialCode("MATERIAL-001"),
            25m);

        recipe.AddMaterialRequirement(requirement);

        Assert.Same(requirement, Assert.Single(recipe.MaterialRequirements));
    }

    [Fact]
    public void AddEquipmentRequirement_AddsOwnedRequirement()
    {
        var recipe = CreateRecipe();
        var requirement = new RecipeEquipmentRequirement(
            recipe.RecipeId,
            new EquipmentCode("EQUIPMENT-001"));

        recipe.AddEquipmentRequirement(requirement);

        Assert.Same(requirement, Assert.Single(recipe.EquipmentRequirements));
    }

    private static Recipe CreateRecipe(bool isApproved = true)
    {
        return new Recipe(
            new RecipeId("RECIPE-ID-001"),
            new RecipeCode("RECIPE-001"),
            "Tablet Blend",
            "1.0",
            isApproved);
    }

    private static RecipeStep CreateStep(RecipeId recipeId, string stepId, int stepNumber)
    {
        return new RecipeStep(
            new RecipeStepId(stepId),
            recipeId,
            stepNumber,
            $"Step {stepNumber}",
            $"Perform step {stepNumber}.",
            false);
    }
}
