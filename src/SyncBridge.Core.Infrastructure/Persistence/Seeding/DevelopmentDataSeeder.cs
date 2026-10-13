using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Seeding;

/// <summary>
/// Applies the repeatable fictional Sprint 3 development reference dataset.
/// </summary>
public sealed class DevelopmentDataSeeder(SyncBridgeDbContext context)
{
    private static readonly Guid RecipeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid MaterialId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid EquipmentId = Guid.Parse("30000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Inserts missing fictional reference definitions and demonstration production orders without overwriting existing data.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of newly persisted entries.</returns>
    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        await AddUserIfMissingAsync("demo.operator", "Demo Operator", cancellationToken);
        await AddUserIfMissingAsync("demo.quality", "Demo Quality Reviewer", cancellationToken);

        var material = await context.Materials.SingleOrDefaultAsync(
            row => row.MaterialCode == "MAT-DEMO-001",
            cancellationToken);
        if (material is null)
        {
            material = new MaterialRow
            {
                MaterialId = MaterialId,
                MaterialCode = "MAT-DEMO-001",
                Name = "Fictional Active Ingredient"
            };
            await context.Materials.AddAsync(material, cancellationToken);
        }

        var equipment = await context.Equipment.SingleOrDefaultAsync(
            row => row.EquipmentCode == "EQ-DEMO-001",
            cancellationToken);
        if (equipment is null)
        {
            equipment = new EquipmentRow
            {
                EquipmentId = EquipmentId,
                EquipmentCode = "EQ-DEMO-001",
                Name = "Fictional Blending Unit",
                CalibrationDueDate = new DateOnly(2027, 12, 31)
            };
            await context.Equipment.AddAsync(equipment, cancellationToken);
        }

        var recipe = await context.Recipes.SingleOrDefaultAsync(
            row => row.RecipeCode == "RCP-DEMO-001",
            cancellationToken);
        if (recipe is null)
        {
            recipe = new RecipeRow
            {
                RecipeId = RecipeId,
                RecipeCode = "RCP-DEMO-001",
                Name = "Fictional Tablet Blend",
                Version = "1.0",
                IsApproved = true
            };
            await context.Recipes.AddAsync(recipe, cancellationToken);
        }

        await AddRecipeStepIfMissingAsync(
            recipe.RecipeId,
            Guid.Parse("11000000-0000-0000-0000-000000000001"),
            1,
            "Verify prerequisites",
            "Verify the fictional material and equipment references.",
            false,
            cancellationToken);
        await AddRecipeStepIfMissingAsync(
            recipe.RecipeId,
            Guid.Parse("11000000-0000-0000-0000-000000000002"),
            2,
            "Perform fictional blend",
            "Execute the fictional demonstration blending activity.",
            true,
            cancellationToken);

        if (!await context.RecipeMaterialRequirements.AnyAsync(
                row => row.RecipeId == recipe.RecipeId && row.MaterialId == material.MaterialId,
                cancellationToken))
        {
            await context.RecipeMaterialRequirements.AddAsync(new RecipeMaterialRequirementRow
            {
                RecipeId = recipe.RecipeId,
                MaterialId = material.MaterialId,
                RequiredQuantity = 25.000m
            }, cancellationToken);
        }

        if (!await context.RecipeEquipmentRequirements.AnyAsync(
                row => row.RecipeId == recipe.RecipeId && row.EquipmentId == equipment.EquipmentId,
                cancellationToken))
        {
            await context.RecipeEquipmentRequirements.AddAsync(new RecipeEquipmentRequirementRow
            {
                RecipeId = recipe.RecipeId,
                EquipmentId = equipment.EquipmentId
            }, cancellationToken);
        }

        await AddOrderIfMissingAsync(
            Guid.Parse("40000000-0000-0000-0000-000000000001"),
            "PO-DEMO-001",
            recipe.RecipeId,
            100.000m,
            new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            cancellationToken);
        await AddOrderIfMissingAsync(
            Guid.Parse("40000000-0000-0000-0000-000000000002"),
            "PO-DEMO-002",
            recipe.RecipeId,
            250.000m,
            new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero),
            cancellationToken);

        return await context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddUserIfMissingAsync(
        string userId,
        string displayName,
        CancellationToken cancellationToken)
    {
        if (!await context.AppUsers.AnyAsync(row => row.UserId == userId, cancellationToken))
        {
            await context.AppUsers.AddAsync(new AppUserRow
            {
                UserId = userId,
                UserName = userId,
                DisplayName = displayName,
                IsActive = true
            }, cancellationToken);
        }
    }

    private async Task AddRecipeStepIfMissingAsync(
        Guid recipeId,
        Guid recipeStepId,
        int stepNumber,
        string name,
        string instructions,
        bool requiresSignature,
        CancellationToken cancellationToken)
    {
        if (!await context.RecipeSteps.AnyAsync(
                row => row.RecipeId == recipeId && row.StepNumber == stepNumber,
                cancellationToken))
        {
            await context.RecipeSteps.AddAsync(new RecipeStepRow
            {
                RecipeStepId = recipeStepId,
                RecipeId = recipeId,
                StepNumber = stepNumber,
                Name = name,
                Instructions = instructions,
                RequiresSignature = requiresSignature
            }, cancellationToken);
        }
    }

    private async Task AddOrderIfMissingAsync(
        Guid orderId,
        string orderNumber,
        Guid recipeId,
        decimal quantity,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (!await context.ProductionOrders.AnyAsync(row => row.OrderNumber == orderNumber, cancellationToken))
        {
            await context.ProductionOrders.AddAsync(new ProductionOrderRow
            {
                OrderId = orderId,
                OrderNumber = orderNumber,
                ProductCode = "PRODUCT-DEMO-001",
                ProductName = "Fictional Demonstration Product",
                RecipeId = recipeId,
                Quantity = quantity,
                Status = ProductionOrderStatus.Planned,
                CreatedAt = createdAt
            }, cancellationToken);
        }
    }
}
