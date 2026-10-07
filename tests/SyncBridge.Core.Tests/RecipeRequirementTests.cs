using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class RecipeRequirementTests
{
    [Fact]
    public void MaterialRequirement_WithNullQuantity_PreservesOptionalQuantity()
    {
        var recipeId = new RecipeId("RECIPE-ID-001");
        var materialCode = new MaterialCode("MATERIAL-001");

        var requirement = new RecipeMaterialRequirement(recipeId, materialCode, null);

        Assert.Equal(recipeId, requirement.RecipeId);
        Assert.Equal(materialCode, requirement.MaterialCode);
        Assert.Null(requirement.RequiredQuantity);
    }

    [Fact]
    public void MaterialRequirement_WithPositiveQuantity_PreservesQuantity()
    {
        var requirement = CreateMaterialRequirement(25m);

        Assert.Equal(25m, requirement.RequiredQuantity);
    }

    [Fact]
    public void MaterialRequirement_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMaterialRequirement(0m));
    }

    [Fact]
    public void MaterialRequirement_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMaterialRequirement(-1m));
    }

    [Fact]
    public void EquipmentRequirement_PreservesRecipeAndEquipmentIdentities()
    {
        var recipeId = new RecipeId("RECIPE-ID-001");
        var equipmentCode = new EquipmentCode("EQUIPMENT-001");

        var requirement = new RecipeEquipmentRequirement(recipeId, equipmentCode);

        Assert.Equal(recipeId, requirement.RecipeId);
        Assert.Equal(equipmentCode, requirement.EquipmentCode);
    }

    private static RecipeMaterialRequirement CreateMaterialRequirement(decimal quantity)
    {
        return new RecipeMaterialRequirement(
            new RecipeId("RECIPE-ID-001"),
            new MaterialCode("MATERIAL-001"),
            quantity);
    }
}
