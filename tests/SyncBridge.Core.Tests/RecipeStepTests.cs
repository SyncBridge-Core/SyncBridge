using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class RecipeStepTests
{
    [Fact]
    public void Constructor_WithSignatureNotRequired_PreservesValues()
    {
        var recipeStepId = new RecipeStepId("STEP-001");
        var recipeId = new RecipeId("RECIPE-ID-001");

        var step = new RecipeStep(
            recipeStepId,
            recipeId,
            1,
            "Dispense",
            "Dispense the required material.",
            false);

        Assert.Equal(recipeStepId, step.RecipeStepId);
        Assert.Equal(recipeId, step.RecipeId);
        Assert.Equal(1, step.StepNumber);
        Assert.Equal("Dispense", step.Name);
        Assert.Equal("Dispense the required material.", step.Instructions);
        Assert.False(step.RequiresSignature);
    }

    [Fact]
    public void Constructor_WithSignatureRequired_PreservesSignatureDefinition()
    {
        var step = CreateStep(1, "Dispense", "Dispense the required material.", true);

        Assert.True(step.RequiresSignature);
    }

    [Fact]
    public void Constructor_WithZeroStepNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateStep(0, "Dispense", "Dispense the required material.", false));
    }

    [Fact]
    public void Constructor_WithNegativeStepNumber_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateStep(-1, "Dispense", "Dispense the required material.", false));
    }

    [Fact]
    public void Constructor_WithBlankName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => CreateStep(1, " ", "Dispense the required material.", false));
    }

    [Fact]
    public void Constructor_WithBlankInstructions_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CreateStep(1, "Dispense", " ", false));
    }

    private static RecipeStep CreateStep(
        int stepNumber,
        string name,
        string instructions,
        bool requiresSignature)
    {
        return new RecipeStep(
            new RecipeStepId("STEP-001"),
            new RecipeId("RECIPE-ID-001"),
            stepNumber,
            name,
            instructions,
            requiresSignature);
    }
}
