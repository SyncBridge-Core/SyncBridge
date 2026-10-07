using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents a production batch.
/// </summary>
public sealed class Batch
{
    private readonly List<BatchStepExecution> _stepExecutions = [];
    private readonly List<Deviation> _deviations = [];
    private readonly List<AuditEvent> _auditEvents = [];

    /// <summary>
    /// Initializes a new production batch in the created state.
    /// </summary>
    /// <param name="batchNumber">The batch identifier.</param>
    /// <param name="productionOrderNumber">The associated production order identifier.</param>
    /// <param name="recipeCode">The associated recipe code.</param>
    /// <param name="plannedQuantity">The quantity planned for production.</param>
    /// <param name="unitOfMeasure">The unit used to measure the planned quantity.</param>
    /// <param name="createdBy">The responsible creating operator.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    public Batch(
        BatchNumber batchNumber,
        ProductionOrderNumber productionOrderNumber,
        RecipeCode recipeCode,
        decimal plannedQuantity,
        UnitOfMeasure unitOfMeasure,
        OperatorId createdBy,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(productionOrderNumber);
        ArgumentNullException.ThrowIfNull(recipeCode);
        ArgumentNullException.ThrowIfNull(unitOfMeasure);
        ArgumentNullException.ThrowIfNull(createdBy);

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
        RecordAudit(
            createdBy,
            AuditEventType.BatchCreated,
            nameof(Batch),
            BatchNumber.Value,
            createdAt,
            BatchStatus.Created.ToString());
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
    /// Gets the step executions registered for this Batch.
    /// </summary>
    public IReadOnlyList<BatchStepExecution> StepExecutions => _stepExecutions.ToArray();

    /// <summary>
    /// Gets the deviations associated with this Batch exception path.
    /// </summary>
    public IReadOnlyList<Deviation> Deviations => _deviations.ToArray();

    /// <summary>
    /// Gets the immutable audit evidence produced by this Batch and its associated deviations.
    /// </summary>
    public IReadOnlyList<AuditEvent> AuditEvents => _auditEvents
        .Concat(_deviations.SelectMany(deviation => deviation.AuditEvents))
        .OrderBy(auditEvent => auditEvent.OccurredAt)
        .ToArray();

    /// <summary>
    /// Marks the created batch as ready for execution.
    /// </summary>
    /// <param name="markedReadyBy">The responsible operator.</param>
    /// <param name="markedReadyAt">The transition timestamp.</param>
    public void MarkReady(OperatorId markedReadyBy, DateTimeOffset markedReadyAt)
    {
        ArgumentNullException.ThrowIfNull(markedReadyBy);
        EnsureCanTransitionTo(BatchStatus.Ready, BatchStatus.Created);
        BatchStatus = BatchStatus.Ready;
        RecordBatchTransition(
            markedReadyBy,
            AuditEventType.BatchMarkedReady,
            markedReadyAt);
    }

    /// <summary>
    /// Starts execution of a ready batch when all Recipe prerequisites are verified.
    /// </summary>
    /// <param name="recipe">The applicable Recipe definition.</param>
    /// <param name="materialVerifications">The available material-verification evidence.</param>
    /// <param name="equipmentVerifications">The available equipment-verification evidence.</param>
    /// <param name="startedBy">The responsible operator.</param>
    /// <param name="startedAt">The transition timestamp.</param>
    public void StartProcessing(
        Recipe recipe,
        IEnumerable<MaterialVerification> materialVerifications,
        IEnumerable<EquipmentVerification> equipmentVerifications,
        OperatorId startedBy,
        DateTimeOffset startedAt)
    {
        EnsureCanTransitionTo(BatchStatus.InProgress, BatchStatus.Ready);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(materialVerifications);
        ArgumentNullException.ThrowIfNull(equipmentVerifications);
        ArgumentNullException.ThrowIfNull(startedBy);

        if (recipe.RecipeCode != RecipeCode)
        {
            throw new BatchPrerequisiteVerificationException(
                BatchNumber,
                "The supplied Recipe does not apply to the Batch.");
        }

        var materialEvidence = materialVerifications.ToArray();
        var equipmentEvidence = equipmentVerifications.ToArray();

        foreach (var requirement in recipe.MaterialRequirements)
        {
            var isVerified = materialEvidence.Any(verification =>
                verification is not null &&
                verification.BatchNumber == BatchNumber &&
                verification.MaterialCode == requirement.MaterialCode &&
                verification.VerificationResult == VerificationResult.Passed);

            if (!isVerified)
            {
                throw new BatchPrerequisiteVerificationException(
                    BatchNumber,
                    $"Material '{requirement.MaterialCode.Value}' has not been successfully verified.");
            }
        }

        foreach (var requirement in recipe.EquipmentRequirements)
        {
            var isVerified = equipmentEvidence.Any(verification =>
                verification is not null &&
                verification.BatchNumber == BatchNumber &&
                verification.EquipmentCode == requirement.EquipmentCode &&
                verification.VerificationResult == VerificationResult.Passed);

            if (!isVerified)
            {
                throw new BatchPrerequisiteVerificationException(
                    BatchNumber,
                    $"Equipment '{requirement.EquipmentCode.Value}' has not been successfully verified.");
            }
        }

        BatchStatus = BatchStatus.InProgress;
        foreach (var verification in materialEvidence)
        {
            AddAuditEvidence(verification.AuditEvidence);
        }

        foreach (var verification in equipmentEvidence)
        {
            AddAuditEvidence(verification.AuditEvidence);
        }

        RecordBatchTransition(
            startedBy,
            AuditEventType.BatchProcessingStarted,
            startedAt);
    }

    /// <summary>
    /// Places the in-progress Batch into Exception with one or more originating deviations.
    /// </summary>
    /// <param name="deviations">The deviations that interrupted normal processing.</param>
    /// <param name="enteredBy">The responsible operator.</param>
    /// <param name="enteredAt">The transition timestamp.</param>
    public void EnterException(
        OperatorId enteredBy,
        DateTimeOffset enteredAt,
        params Deviation[] deviations)
    {
        EnsureCanTransitionTo(BatchStatus.Exception, BatchStatus.InProgress);
        ArgumentNullException.ThrowIfNull(enteredBy);
        ArgumentNullException.ThrowIfNull(deviations);

        if (deviations.Length == 0)
        {
            throw new BatchDeviationException(
                BatchNumber,
                "At least one deviation is required to enter Exception.");
        }

        if (deviations.Any(deviation => deviation is null))
        {
            throw new BatchDeviationException(
                BatchNumber,
                "Deviation evidence cannot contain null entries.");
        }

        if (deviations.Any(deviation => deviation.BatchNumber != BatchNumber))
        {
            throw new BatchDeviationException(
                BatchNumber,
                "Every deviation must originate from this Batch.");
        }

        if (deviations.Any(deviation => !deviation.IsBlocking))
        {
            throw new BatchDeviationException(
                BatchNumber,
                "Every deviation must be unresolved when the Batch enters Exception.");
        }

        if (deviations
            .GroupBy(deviation => deviation.DeviationCode)
            .Any(group => group.Count() > 1))
        {
            throw new BatchDeviationException(
                BatchNumber,
                "Duplicate deviation identities are not permitted.");
        }

        _deviations.AddRange(deviations);
        BatchStatus = BatchStatus.Exception;
        RecordBatchTransition(
            enteredBy,
            AuditEventType.BatchExceptionEntered,
            enteredAt,
            $"Deviation count: {deviations.Length}");
    }

    /// <summary>
    /// Resumes execution after every associated deviation has been resolved.
    /// </summary>
    /// <param name="resumedBy">The responsible operator.</param>
    /// <param name="resumedAt">The transition timestamp.</param>
    public void ResumeProcessing(OperatorId resumedBy, DateTimeOffset resumedAt)
    {
        EnsureCanTransitionTo(BatchStatus.InProgress, BatchStatus.Exception);
        ArgumentNullException.ThrowIfNull(resumedBy);

        if (_deviations.Any(deviation => deviation.IsBlocking))
        {
            throw new BatchDeviationException(
                BatchNumber,
                "All associated deviations must be resolved before processing can resume.");
        }

        BatchStatus = BatchStatus.InProgress;
        RecordBatchTransition(
            resumedBy,
            AuditEventType.BatchProcessingResumed,
            resumedAt);
    }

    /// <summary>
    /// Completes the in-progress batch.
    /// </summary>
    /// <param name="recipe">The applicable Recipe definition.</param>
    /// <param name="completedBy">The responsible operator.</param>
    /// <param name="completedAt">The transition timestamp.</param>
    public void Complete(
        Recipe recipe,
        OperatorId completedBy,
        DateTimeOffset completedAt)
    {
        EnsureCanTransitionTo(BatchStatus.Completed, BatchStatus.InProgress);
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(completedBy);
        EnsureRecipeApplies(recipe);

        var allRequiredStepsCompleted = recipe.Steps.All(step =>
            _stepExecutions.Any(execution =>
                execution.RecipeStepId == step.RecipeStepId &&
                execution.StepExecutionStatus == StepExecutionStatus.Completed));

        if (!allRequiredStepsCompleted)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "Every required Recipe step must be completed before the Batch can complete.");
        }

        if (_deviations.Any(deviation => deviation.IsBlocking))
        {
            throw new BatchDeviationException(
                BatchNumber,
                "Every associated deviation must be resolved before the Batch can complete.");
        }

        BatchStatus = BatchStatus.Completed;
        RecordBatchTransition(
            completedBy,
            AuditEventType.BatchCompleted,
            completedAt);
    }

    /// <summary>
    /// Creates and registers a Pending execution for a Recipe step applicable to this Batch.
    /// </summary>
    /// <param name="recipe">The applicable Recipe definition.</param>
    /// <param name="step">The Recipe step to execute.</param>
    /// <param name="batchStepExecutionId">The step-execution identity.</param>
    /// <returns>The registered Pending step execution.</returns>
    public BatchStepExecution CreateStepExecution(
        Recipe recipe,
        RecipeStep step,
        BatchStepExecutionId batchStepExecutionId)
    {
        EnsureBatchIsNotCompleted();
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(batchStepExecutionId);
        EnsureRecipeApplies(recipe);

        var belongsToRecipe = step.RecipeId == recipe.RecipeId &&
            recipe.Steps.Any(recipeStep =>
                recipeStep.RecipeStepId == step.RecipeStepId &&
                recipeStep.StepNumber == step.StepNumber);

        if (!belongsToRecipe)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "The RecipeStep does not belong to the applicable Recipe.");
        }

        if (_stepExecutions.Any(execution =>
            execution.BatchStepExecutionId == batchStepExecutionId ||
            execution.RecipeStepId == step.RecipeStepId))
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "The RecipeStep already has an execution registered for this Batch.");
        }

        var execution = new BatchStepExecution(
            batchStepExecutionId,
            BatchNumber,
            step.RecipeStepId,
            step.StepNumber,
            step.RequiresSignature);

        _stepExecutions.Add(execution);
        return execution;
    }

    /// <summary>
    /// Starts the next eligible Pending Recipe-step execution.
    /// </summary>
    /// <param name="recipe">The applicable Recipe definition.</param>
    /// <param name="execution">The registered execution to start.</param>
    /// <param name="startedAt">The execution start timestamp.</param>
    /// <param name="startedBy">The responsible operator.</param>
    public void StartStep(
        Recipe recipe,
        BatchStepExecution execution,
        DateTimeOffset startedAt,
        OperatorId startedBy)
    {
        EnsureStepOperationsAreAllowed();
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(startedBy);
        EnsureRecipeApplies(recipe);
        EnsureExecutionIsRegistered(execution);

        var stepBelongsToRecipe = recipe.Steps.Any(step =>
            step.RecipeStepId == execution.RecipeStepId &&
            step.StepNumber == execution.StepNumber);

        if (!stepBelongsToRecipe)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "The step execution does not belong to the applicable Recipe.");
        }

        if (_stepExecutions.Any(candidate =>
            candidate != execution &&
            candidate.StepExecutionStatus == StepExecutionStatus.InProgress))
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "Another Recipe step is already in progress.");
        }

        foreach (var earlierStep in recipe.Steps.Where(step => step.StepNumber < execution.StepNumber))
        {
            var earlierStepCompleted = _stepExecutions.Any(candidate =>
                candidate.RecipeStepId == earlierStep.RecipeStepId &&
                candidate.StepExecutionStatus == StepExecutionStatus.Completed);

            if (!earlierStepCompleted)
            {
                throw new InvalidBatchStepExecutionException(
                    BatchNumber,
                    $"Recipe step {earlierStep.StepNumber} must be completed first.");
            }
        }

        execution.Start(startedAt);
        RecordAudit(
            startedBy,
            AuditEventType.StepStarted,
            nameof(BatchStepExecution),
            execution.BatchStepExecutionId.Value,
            startedAt,
            StepExecutionStatus.InProgress.ToString());
    }

    /// <summary>
    /// Completes the active Recipe-step execution.
    /// </summary>
    /// <param name="execution">The registered execution to complete.</param>
    /// <param name="completedAt">The execution completion timestamp.</param>
    /// <param name="completedBy">The responsible operator identity.</param>
    /// <param name="electronicSignature">Optional electronic-signature evidence.</param>
    public void CompleteStep(
        BatchStepExecution execution,
        DateTimeOffset completedAt,
        OperatorId completedBy,
        ElectronicSignature? electronicSignature = null)
    {
        EnsureStepOperationsAreAllowed();
        ArgumentNullException.ThrowIfNull(execution);
        ArgumentNullException.ThrowIfNull(completedBy);
        EnsureExecutionIsRegistered(execution);

        if (execution.RequiresSignature)
        {
            EnsureSignatureApplies(execution, electronicSignature);
        }

        execution.Complete(completedAt, completedBy);
        if (electronicSignature is not null)
        {
            AddAuditEvidence(electronicSignature.AuditEvidence);
        }

        RecordAudit(
            completedBy,
            AuditEventType.StepCompleted,
            nameof(BatchStepExecution),
            execution.BatchStepExecutionId.Value,
            completedAt,
            StepExecutionStatus.Completed.ToString());
    }

    private void EnsureSignatureApplies(
        BatchStepExecution execution,
        ElectronicSignature? electronicSignature)
    {
        if (electronicSignature is null)
        {
            throw new ElectronicSignatureValidationException(
                BatchNumber,
                $"Step {execution.StepNumber} requires an electronic signature.");
        }

        if (electronicSignature.BatchNumber != BatchNumber)
        {
            throw new ElectronicSignatureValidationException(
                BatchNumber,
                "The electronic signature belongs to another Batch.");
        }

        if (electronicSignature.RelatedRecordType != RelatedRecordType.BatchStepExecution)
        {
            throw new ElectronicSignatureValidationException(
                BatchNumber,
                "The electronic signature does not target a BatchStepExecution record.");
        }

        if (electronicSignature.RelatedRecordId.Value != execution.BatchStepExecutionId.Value)
        {
            throw new ElectronicSignatureValidationException(
                BatchNumber,
                "The electronic signature targets another step execution.");
        }
    }

    private void EnsureRecipeApplies(Recipe recipe)
    {
        if (recipe.RecipeCode != RecipeCode)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "The supplied Recipe does not apply to the Batch.");
        }
    }

    private void EnsureExecutionIsRegistered(BatchStepExecution execution)
    {
        if (!_stepExecutions.Contains(execution) || execution.BatchNumber != BatchNumber)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "The step execution is not registered for this Batch.");
        }
    }

    private void EnsureStepOperationsAreAllowed()
    {
        if (BatchStatus != BatchStatus.InProgress)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "Recipe steps can execute only while the Batch is InProgress.");
        }
    }

    private void EnsureBatchIsNotCompleted()
    {
        if (BatchStatus == BatchStatus.Completed)
        {
            throw new InvalidBatchStepExecutionException(
                BatchNumber,
                "Completed Batches cannot be modified.");
        }
    }

    private void RecordBatchTransition(
        OperatorId operatorId,
        AuditEventType action,
        DateTimeOffset occurredAt,
        string? details = null)
    {
        RecordAudit(
            operatorId,
            action,
            nameof(Batch),
            BatchNumber.Value,
            occurredAt,
            BatchStatus.ToString(),
            details);
    }

    private void RecordAudit(
        OperatorId operatorId,
        AuditEventType action,
        string affectedEntity,
        string affectedEntityId,
        DateTimeOffset occurredAt,
        string? resultingState = null,
        string? details = null)
    {
        _auditEvents.Add(AuditEvent.Create(
            BatchNumber,
            operatorId,
            action,
            affectedEntity,
            affectedEntityId,
            occurredAt,
            resultingState,
            details));
    }

    private void AddAuditEvidence(AuditEvent auditEvent)
    {
        if (_auditEvents.All(existing => existing.AuditEventId != auditEvent.AuditEventId))
        {
            _auditEvents.Add(auditEvent);
        }
    }

    private void EnsureCanTransitionTo(BatchStatus targetStatus, BatchStatus requiredStatus)
    {
        if (BatchStatus != requiredStatus)
        {
            throw new InvalidBatchStateTransitionException(
                BatchNumber,
                BatchStatus,
                targetStatus);
        }
    }
}
