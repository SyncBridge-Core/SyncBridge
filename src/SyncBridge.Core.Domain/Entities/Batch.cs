using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production batch.
/// </summary>
public sealed class Batch
{
    public Batch(
        BatchNumber batchNumber,
        ProductionOrderNumber productionOrderNumber,
        RecipeCode recipeCode,
        BatchStatus status,
        decimal plannedQuantity,
        UnitOfMeasure unitOfMeasure)
    {
        BatchNumber = batchNumber;
        ProductionOrderNumber = productionOrderNumber;
        RecipeCode = recipeCode;
        BatchStatus = status;
        PlannedQuantity = plannedQuantity;
        UnitOfMeasure = unitOfMeasure;
    }

    public BatchNumber BatchNumber { get; private set; }

    public ProductionOrderNumber ProductionOrderNumber { get; private set; }

    public RecipeCode RecipeCode { get; private set; }

    public BatchStatus BatchStatus { get; private set; }

    public decimal PlannedQuantity { get; private set; }

    public UnitOfMeasure UnitOfMeasure { get; private set; }
}
