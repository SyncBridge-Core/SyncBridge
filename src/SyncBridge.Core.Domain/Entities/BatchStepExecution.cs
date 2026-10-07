using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents runtime execution of one Recipe step for one Batch.
/// </summary>
public sealed class BatchStepExecution
{
    /// <summary>
    /// Initializes a Pending Recipe-step execution for a Batch.
    /// </summary>
    /// <param name="batchStepExecutionId">The step-execution identity.</param>
    /// <param name="batchNumber">The associated Batch identity.</param>
    /// <param name="recipeStepId">The associated Recipe-step identity.</param>
    /// <param name="stepNumber">The prescribed step number.</param>
    /// <param name="requiresSignature">Whether completion requires electronic-signature evidence.</param>
    public BatchStepExecution(
        BatchStepExecutionId batchStepExecutionId,
        BatchNumber batchNumber,
        RecipeStepId recipeStepId,
        int stepNumber,
        bool requiresSignature)
    {
        ArgumentNullException.ThrowIfNull(batchStepExecutionId);
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(recipeStepId);

        if (stepNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stepNumber),
                "Step number must be greater than zero.");
        }

        BatchStepExecutionId = batchStepExecutionId;
        BatchNumber = batchNumber;
        RecipeStepId = recipeStepId;
        StepNumber = stepNumber;
        RequiresSignature = requiresSignature;
        StepExecutionStatus = StepExecutionStatus.Pending;
    }

    /// <summary>
    /// Gets the step-execution identity.
    /// </summary>
    public BatchStepExecutionId BatchStepExecutionId { get; }

    /// <summary>
    /// Gets the associated Batch identity.
    /// </summary>
    public BatchNumber BatchNumber { get; }

    /// <summary>
    /// Gets the associated Recipe-step identity.
    /// </summary>
    public RecipeStepId RecipeStepId { get; }

    /// <summary>
    /// Gets the prescribed step number.
    /// </summary>
    public int StepNumber { get; }

    /// <summary>
    /// Gets whether completion requires electronic-signature evidence.
    /// </summary>
    public bool RequiresSignature { get; }

    /// <summary>
    /// Gets the current step-execution status.
    /// </summary>
    public StepExecutionStatus StepExecutionStatus { get; private set; }

    /// <summary>
    /// Gets the timestamp at which execution started, when available.
    /// </summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp at which execution completed, when available.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the operator who completed execution, when available.
    /// </summary>
    public OperatorId? CompletedBy { get; private set; }

    internal void Start(DateTimeOffset startedAt)
    {
        if (StepExecutionStatus != StepExecutionStatus.Pending)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                $"Step {StepNumber} can start only from Pending.");
        }

        StartedAt = startedAt;
        StepExecutionStatus = StepExecutionStatus.InProgress;
    }

    internal void Complete(DateTimeOffset completedAt, OperatorId completedBy)
    {
        ArgumentNullException.ThrowIfNull(completedBy);

        if (StepExecutionStatus != StepExecutionStatus.InProgress)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                $"Step {StepNumber} can complete only from InProgress.");
        }

        CompletedAt = completedAt;
        CompletedBy = completedBy;
        StepExecutionStatus = StepExecutionStatus.Completed;
    }
}
