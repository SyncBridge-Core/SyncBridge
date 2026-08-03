using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production batch.
/// </summary>
public sealed class Batch
{
    /// <summary>
    /// Initializes a new production batch in the created state.
    /// </summary>
    /// <param name="batchNumber">The batch identifier.</param>
    /// <param name="productionOrderNumber">The associated production order identifier.</param>
    /// <param name="recipeCode">The associated recipe code.</param>
    /// <param name="plannedQuantity">The quantity planned for production.</param>
    /// <param name="unitOfMeasure">The unit used to measure the planned quantity.</param>
    public Batch(
        BatchNumber batchNumber,
        ProductionOrderNumber productionOrderNumber,
        RecipeCode recipeCode,
        decimal plannedQuantity,
        UnitOfMeasure unitOfMeasure)
    {
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(productionOrderNumber);
        ArgumentNullException.ThrowIfNull(recipeCode);
        ArgumentNullException.ThrowIfNull(unitOfMeasure);

        if (plannedQuantity <= 0)
        {
            throw new DomainException("Planned quantity must be greater than zero.");
        }

        BatchNumber = batchNumber;
        ProductionOrderNumber = productionOrderNumber;
        RecipeCode = recipeCode;
        BatchStatus = BatchStatus.Created;
        PlannedQuantity = plannedQuantity;
        UnitOfMeasure = unitOfMeasure;
    }

    /// <summary>
    /// Gets the batch identifier.
    /// </summary>
    public BatchNumber BatchNumber { get; private set; }

    /// <summary>
    /// Gets the associated production order identifier.
    /// </summary>
    public ProductionOrderNumber ProductionOrderNumber { get; private set; }

    /// <summary>
    /// Gets the associated recipe code.
    /// </summary>
    public RecipeCode RecipeCode { get; private set; }

    /// <summary>
    /// Gets the current batch status.
    /// </summary>
    public BatchStatus BatchStatus { get; private set; }

    /// <summary>
    /// Gets the quantity planned for production.
    /// </summary>
    public decimal PlannedQuantity { get; private set; }

    /// <summary>
    /// Gets the unit used to measure the planned quantity.
    /// </summary>
    public UnitOfMeasure UnitOfMeasure { get; private set; }

    /// <summary>
    /// Prepares the batch for manufacturing verification.
    /// </summary>
    public void Prepare()
    {
        TransitionTo(BatchStatus.Prepared, BatchStatus.Created);
    }

    /// <summary>
    /// Confirms a successful material verification for the batch.
    /// </summary>
    /// <param name="verification">The material verification to confirm.</param>
    public void ConfirmMaterialVerification(MaterialVerification verification)
    {
        ArgumentNullException.ThrowIfNull(verification);
        EnsureVerificationBelongsToBatch(verification.BatchNumber);

        if (verification.VerificationResult != VerificationResult.Passed)
        {
            throw new DomainException("Material verification must have a passed result.");
        }

        TransitionTo(BatchStatus.MaterialVerified, BatchStatus.Prepared);
    }

    /// <summary>
    /// Confirms a successful equipment verification for the batch.
    /// </summary>
    /// <param name="verification">The equipment verification to confirm.</param>
    public void ConfirmEquipmentVerification(EquipmentVerification verification)
    {
        ArgumentNullException.ThrowIfNull(verification);
        EnsureVerificationBelongsToBatch(verification.BatchNumber);

        if (verification.VerificationResult != VerificationResult.Passed)
        {
            throw new DomainException("Equipment verification must have a passed result.");
        }

        TransitionTo(BatchStatus.EquipmentVerified, BatchStatus.MaterialVerified);
    }

    /// <summary>
    /// Starts processing the fully verified batch.
    /// </summary>
    public void StartProcessing()
    {
        TransitionTo(BatchStatus.InProcess, BatchStatus.EquipmentVerified);
    }

    /// <summary>
    /// Submits the in-process batch for quality review.
    /// </summary>
    public void SubmitForQualityReview()
    {
        TransitionTo(BatchStatus.QualityReview, BatchStatus.InProcess);
    }

    /// <summary>
    /// Completes the batch following quality review.
    /// </summary>
    public void Complete()
    {
        TransitionTo(BatchStatus.Completed, BatchStatus.QualityReview);
    }

    /// <summary>
    /// Cancels a batch that has not reached a terminal state.
    /// </summary>
    public void Cancel()
    {
        if (BatchStatus is BatchStatus.Completed or BatchStatus.Cancelled)
        {
            throw new InvalidBatchStateTransitionException(
                BatchNumber,
                BatchStatus,
                BatchStatus.Cancelled);
        }

        BatchStatus = BatchStatus.Cancelled;
    }

    private void EnsureVerificationBelongsToBatch(BatchNumber verificationBatchNumber)
    {
        if (verificationBatchNumber != BatchNumber)
        {
            throw new DomainException("Verification must belong to the batch.");
        }
    }

    private void TransitionTo(BatchStatus targetStatus, BatchStatus requiredStatus)
    {
        if (BatchStatus != requiredStatus)
        {
            throw new InvalidBatchStateTransitionException(
                BatchNumber,
                BatchStatus,
                targetStatus);
        }

        BatchStatus = targetStatus;
    }
}
