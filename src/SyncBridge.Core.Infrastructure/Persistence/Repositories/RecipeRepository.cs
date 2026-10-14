using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Assembly;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Repositories;

internal sealed class RecipeRepository(SyncBridgeDbContext context) : IRecipeRepository
{
    public async Task<Recipe?> GetByIdAsync(
        RecipeId recipeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipeId);
        return await LoadAsync(
            TechnicalGuidConverter.ToGuid(recipeId.Value),
            null,
            cancellationToken);
    }

    public async Task<Recipe?> GetByCodeAsync(
        RecipeCode recipeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipeCode);
        return await LoadAsync(null, recipeCode.Value, cancellationToken);
    }

    private async Task<Recipe?> LoadAsync(
        Guid? recipeId,
        string? recipeCode,
        CancellationToken cancellationToken)
    {
        var recipeRow = await context.Recipes.AsNoTracking().SingleOrDefaultAsync(
            row => (recipeId == null || row.RecipeId == recipeId) &&
                (recipeCode == null || row.RecipeCode == recipeCode),
            cancellationToken);
        if (recipeRow is null)
        {
            return null;
        }

        var steps = await context.RecipeSteps.AsNoTracking()
            .Where(row => row.RecipeId == recipeRow.RecipeId)
            .OrderBy(row => row.StepNumber)
            .ToArrayAsync(cancellationToken);
        var materialRequirements = await context.RecipeMaterialRequirements.AsNoTracking()
            .Where(row => row.RecipeId == recipeRow.RecipeId)
            .ToArrayAsync(cancellationToken);
        var equipmentRequirements = await context.RecipeEquipmentRequirements.AsNoTracking()
            .Where(row => row.RecipeId == recipeRow.RecipeId)
            .ToArrayAsync(cancellationToken);
        var materialIds = materialRequirements.Select(row => row.MaterialId).ToArray();
        var equipmentIds = equipmentRequirements.Select(row => row.EquipmentId).ToArray();
        var materials = await context.Materials.AsNoTracking()
            .Where(row => materialIds.Contains(row.MaterialId))
            .ToDictionaryAsync(row => row.MaterialId, cancellationToken);
        var equipment = await context.Equipment.AsNoTracking()
            .Where(row => equipmentIds.Contains(row.EquipmentId))
            .ToDictionaryAsync(row => row.EquipmentId, cancellationToken);

        try
        {
            return RecipeAssembler.Assemble(
                recipeRow,
                steps,
                materialRequirements,
                equipmentRequirements,
                materials,
                equipment);
        }
        catch (Exception exception) when (exception is DomainException or ArgumentException)
        {
            throw new PersistenceIntegrityException("The persisted Recipe definition is invalid.", exception);
        }
    }
}
