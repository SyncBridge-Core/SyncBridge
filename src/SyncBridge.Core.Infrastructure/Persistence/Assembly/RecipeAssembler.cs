using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Assembly;

internal static class RecipeAssembler
{
    internal static Recipe Assemble(
        RecipeRow recipeRow,
        IEnumerable<RecipeStepRow> stepRows,
        IEnumerable<RecipeMaterialRequirementRow> materialRequirementRows,
        IEnumerable<RecipeEquipmentRequirementRow> equipmentRequirementRows,
        IReadOnlyDictionary<Guid, MaterialRow> materials,
        IReadOnlyDictionary<Guid, EquipmentRow> equipment)
    {
        ArgumentNullException.ThrowIfNull(recipeRow);
        ArgumentNullException.ThrowIfNull(stepRows);
        ArgumentNullException.ThrowIfNull(materialRequirementRows);
        ArgumentNullException.ThrowIfNull(equipmentRequirementRows);
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(equipment);

        var recipeId = new RecipeId(TechnicalGuidConverter.ToCanonicalString(recipeRow.RecipeId));
        var recipe = new Recipe(
            recipeId,
            new RecipeCode(DbdsStringLengthValidator.RequireWithinLimit(recipeRow.RecipeCode, 40, nameof(recipeRow.RecipeCode))),
            DbdsStringLengthValidator.RequireWithinLimit(recipeRow.Name, 160, nameof(recipeRow.Name)),
            DbdsStringLengthValidator.RequireWithinLimit(recipeRow.Version, 32, nameof(recipeRow.Version)),
            recipeRow.IsApproved);

        foreach (var stepRow in stepRows.OrderBy(row => row.StepNumber))
        {
            if (stepRow.RecipeId != recipeRow.RecipeId)
            {
                throw new PersistenceIntegrityException("A RecipeStep references the wrong Recipe.");
            }

            recipe.AddStep(new RecipeStep(
                new RecipeStepId(TechnicalGuidConverter.ToCanonicalString(stepRow.RecipeStepId)),
                recipeId,
                stepRow.StepNumber,
                DbdsStringLengthValidator.RequireWithinLimit(stepRow.Name, 160, nameof(stepRow.Name)),
                DbdsStringLengthValidator.RequireWithinLimit(stepRow.Instructions, 2000, nameof(stepRow.Instructions)),
                stepRow.RequiresSignature));
        }

        foreach (var requirementRow in materialRequirementRows)
        {
            if (!materials.TryGetValue(requirementRow.MaterialId, out var material))
            {
                throw new PersistenceIntegrityException("A Recipe material requirement references a missing Material.");
            }

            recipe.AddMaterialRequirement(new RecipeMaterialRequirement(
                recipeId,
                new MaterialCode(DbdsStringLengthValidator.RequireWithinLimit(material.MaterialCode, 40, nameof(material.MaterialCode))),
                requirementRow.RequiredQuantity));
        }

        foreach (var requirementRow in equipmentRequirementRows)
        {
            if (!equipment.TryGetValue(requirementRow.EquipmentId, out var equipmentRow))
            {
                throw new PersistenceIntegrityException("A Recipe equipment requirement references missing Equipment.");
            }

            recipe.AddEquipmentRequirement(new RecipeEquipmentRequirement(
                recipeId,
                new EquipmentCode(DbdsStringLengthValidator.RequireWithinLimit(equipmentRow.EquipmentCode, 40, nameof(equipmentRow.EquipmentCode)))));
        }

        return recipe;
    }
}
