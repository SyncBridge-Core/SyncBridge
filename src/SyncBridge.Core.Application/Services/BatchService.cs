using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Services;

/// <summary>Secured manufacturing orchestration; eligibility remains in existing Domain operations.</summary>
public sealed class BatchService(ApplicationOperation operations, IBatchRepository batches,
    IProductionOrderRepository orders, IRecipeRepository recipes, TimeProvider time)
{
    private static readonly string[] ExecutionRoles = [ManufacturingRoles.Operator];
    private static readonly string[] ReadRoles = [ManufacturingRoles.Operator, ManufacturingRoles.Supervisor, ManufacturingRoles.Quality];
    private static readonly string[] ReviewRoles = [ManufacturingRoles.Supervisor, ManufacturingRoles.Quality];

    public Task<OperationResult<BatchView>> InitiateAsync(string orderNumber, string batchNumber, CancellationToken cancellationToken = default) =>
        operations.RunAsync<BatchView>(ExecutionRoles, true, async (actor, ct) =>
        {
            var number = Number(batchNumber);
            var order = ApplicationOperation.Require(await orders.GetByNumberAsync(new ProductionOrderNumber(ApplicationOperation.Text(orderNumber, 40)), ct));
            ApplicationOperation.RequireRule(order.ProductionOrderStatus is ProductionOrderStatus.Released or ProductionOrderStatus.InProcess);
            var recipe = ApplicationOperation.Require(await recipes.GetByIdAsync(order.RecipeId, ct));
            ApplicationOperation.RequireRule(recipe.IsApproved && await batches.GetExecutionAggregateAsync(number, ct) is null);
            var batch = new Batch(number, order, recipe.RecipeCode, actor.ActorId, time.GetUtcNow());
            await batches.AddAsync(batch, ct);
            return View(batch, recipe, new([], [], [], new Dictionary<string, string?>()));
        }, cancellationToken);

    public Task<OperationResult<BatchView>> GetAsync(string batchNumber, CancellationToken cancellationToken = default) =>
        operations.RunAsync<BatchView>(ReadRoles, false, async (_, ct) =>
        {
            var batch = await LoadAsync(batchNumber, ct);
            var recipe = ApplicationOperation.Require(await recipes.GetByCodeAsync(batch.RecipeCode, ct));
            return View(batch, recipe, await batches.GetEvidenceAsync(batch, ct));
        }, cancellationToken);

    public Task<OperationResult<BatchView>> MarkReadyAsync(string batchNumber, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, (batch, _, actor, _) =>
        {
            batch.MarkReady(actor.ActorId, time.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<OperationResult<BatchView>> StartProcessingAsync(string batchNumber, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, async (batch, recipe, actor, ct) =>
        {
            var evidence = await batches.GetEvidenceAsync(batch, ct);
            batch.StartProcessing(recipe, evidence.Materials, evidence.Equipment, actor.ActorId, time.GetUtcNow());
        }, cancellationToken);

    public Task<OperationResult<BatchView>> VerifyMaterialAsync(string batchNumber, string materialCode, VerificationResult result, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, async (batch, recipe, actor, ct) =>
        {
            RequireVerificationPhase(batch, result);
            var code = new MaterialCode(ApplicationOperation.Text(materialCode, 40));
            ApplicationOperation.RequireRule(recipe.MaterialRequirements.Any(item => item.MaterialCode == code));
            await batches.SaveMaterialVerificationAsync(new MaterialVerification(batch.BatchNumber, code, actor.ActorId, time.GetUtcNow(), result), ct);
        }, cancellationToken);

    public Task<OperationResult<BatchView>> VerifyEquipmentAsync(string batchNumber, string equipmentCode, VerificationResult result, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, async (batch, recipe, actor, ct) =>
        {
            RequireVerificationPhase(batch, result);
            var code = new EquipmentCode(ApplicationOperation.Text(equipmentCode, 40));
            ApplicationOperation.RequireRule(recipe.EquipmentRequirements.Any(item => item.EquipmentCode == code));
            await batches.SaveEquipmentVerificationAsync(new EquipmentVerification(batch.BatchNumber, code, actor.ActorId, time.GetUtcNow(), result), ct);
        }, cancellationToken);

    public Task<OperationResult<BatchView>> StartStepAsync(string batchNumber, int stepNumber, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, (batch, recipe, actor, _) =>
        {
            var step = ApplicationOperation.Require(recipe.Steps.SingleOrDefault(item => item.StepNumber == stepNumber));
            var execution = batch.StepExecutions.SingleOrDefault(item => item.StepNumber == stepNumber) ??
                batch.CreateStepExecution(recipe, step, new BatchStepExecutionId(Guid.NewGuid().ToString("D")));
            batch.StartStep(recipe, execution, time.GetUtcNow(), actor.ActorId);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<OperationResult<BatchView>> SignStepAsync(string batchNumber, string executionId, ElectronicSignatureMeaning meaning, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, [ManufacturingRoles.Operator, ManufacturingRoles.Quality], async (batch, _, actor, ct) =>
        {
            var execution = Execution(batch, executionId);
            ApplicationOperation.RequireRule(batch.BatchStatus == BatchStatus.InProgress && execution.StepExecutionStatus == StepExecutionStatus.InProgress);
            var signature = new ElectronicSignature(new ElectronicSignatureId(Guid.NewGuid().ToString("D")), batch.BatchNumber,
                RelatedRecordType.BatchStepExecution, new RelatedRecordId(execution.BatchStepExecutionId.Value), actor.ActorId, meaning, time.GetUtcNow());
            await batches.SaveSignatureAsync(signature, ct);
        }, cancellationToken);

    public Task<OperationResult<BatchView>> CompleteStepAsync(string batchNumber, string executionId, string? signatureId = null, string? recordedData = null, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, async (batch, _, actor, ct) =>
        {
            var execution = Execution(batch, executionId);
            if (recordedData is not null) ApplicationOperation.Text(recordedData, 2000);
            ElectronicSignature? signature = null;
            if (signatureId is not null)
            {
                CanonicalId(signatureId);
                var evidence = await batches.GetEvidenceAsync(batch, ct);
                signature = ApplicationOperation.Require(evidence.Signatures.SingleOrDefault(item => item.ElectronicSignatureId.Value == signatureId));
                ApplicationOperation.RequireRule(signature.RelatedRecordId.Value == executionId && signature.BatchNumber == batch.BatchNumber);
            }
            batch.CompleteStep(execution, time.GetUtcNow(), actor.ActorId, signature);
            if (recordedData is not null) await batches.RecordStepDataAsync(execution.BatchStepExecutionId, recordedData, ct);
        }, cancellationToken);

    public Task<OperationResult<BatchView>> CreateDeviationAsync(string batchNumber, string deviationCode, string description,
        string? executionId = null, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, (batch, _, actor, _) =>
        {
            var code = new DeviationCode(ApplicationOperation.Text(deviationCode, 40));
            var now = time.GetUtcNow();
            var deviation = new Deviation(code, batch.BatchNumber, executionId is null ? null : Execution(batch, executionId),
                ApplicationOperation.Text(description, 2000), actor.ActorId, now);
            batch.EnterException(actor.ActorId, now, deviation);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<OperationResult<BatchView>> ReviewDeviationAsync(string batchNumber, string deviationCode, DeviationAction action,
        string? commentsOrDisposition = null, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, [ManufacturingRoles.Quality], (batch, _, actor, _) =>
        {
            var code = ApplicationOperation.Text(deviationCode, 40);
            var deviation = ApplicationOperation.Require(batch.Deviations.SingleOrDefault(item => item.DeviationCode.Value == code));
            var now = time.GetUtcNow();
            switch (action)
            {
                case DeviationAction.BeginReview: deviation.BeginReview(ApplicationOperation.Text(commentsOrDisposition!, 2000), actor.ActorId, now); break;
                case DeviationAction.Approve: deviation.Approve(actor.ActorId, now); break;
                case DeviationAction.Reject: deviation.Reject(actor.ActorId, now); break;
                case DeviationAction.Resolve: deviation.Resolve(ApplicationOperation.Text(commentsOrDisposition!, 1000), now, actor.ActorId); break;
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<OperationResult<BatchView>> ResumeAsync(string batchNumber, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, ExecutionRoles, (batch, _, actor, _) =>
        {
            batch.ResumeProcessing(actor.ActorId, time.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<OperationResult<BatchView>> CompleteAsync(string batchNumber, CancellationToken cancellationToken = default) =>
        MutateAsync(batchNumber, [ManufacturingRoles.Supervisor], (batch, recipe, actor, _) =>
        {
            batch.Complete(recipe, actor.ActorId, time.GetUtcNow());
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<OperationResult<IReadOnlyList<AuditView>>> GetAuditAsync(string batchNumber, CancellationToken cancellationToken = default) =>
        operations.RunAsync<IReadOnlyList<AuditView>>(ReviewRoles, false,
            async (_, ct) => (await LoadAsync(batchNumber, ct)).AuditEvents.Select(AuditView).ToArray(), cancellationToken);

    public Task<OperationResult<IReadOnlyList<ExceptionReviewView>>> ReviewExceptionsAsync(CancellationToken cancellationToken = default) =>
        operations.RunAsync<IReadOnlyList<ExceptionReviewView>>(ReviewRoles, false, async (_, ct) =>
        {
            var results = new List<ExceptionReviewView>();
            foreach (var number in await batches.GetReviewCandidatesAsync(ct))
            {
                var batch = await LoadAsync(number.Value, ct);
                var recipe = ApplicationOperation.Require(await recipes.GetByCodeAsync(batch.RecipeCode, ct));
                var evidence = await batches.GetEvidenceAsync(batch, ct);
                var deviations = batch.Deviations.Where(item => item.IsBlocking).Select(item => item.DeviationCode.Value).ToArray();
                var materials = recipe.MaterialRequirements.Where(item => !evidence.Materials.Any(v => v.MaterialCode == item.MaterialCode && v.VerificationResult == VerificationResult.Passed)).Select(item => item.MaterialCode.Value).ToArray();
                var equipment = recipe.EquipmentRequirements.Where(item => !evidence.Equipment.Any(v => v.EquipmentCode == item.EquipmentCode && v.VerificationResult == VerificationResult.Passed)).Select(item => item.EquipmentCode.Value).ToArray();
                if (deviations.Length + materials.Length + equipment.Length > 0) results.Add(new(number.Value, deviations, materials, equipment));
            }
            return results.ToArray();
        }, cancellationToken);

    private Task<OperationResult<BatchView>> MutateAsync(string batchNumber, string[] roles,
        Func<Batch, Recipe, AuthenticatedActor, CancellationToken, Task> command, CancellationToken cancellationToken) =>
        operations.RunAsync<BatchView>(roles, true, async (actor, ct) =>
        {
            var batch = await LoadAsync(batchNumber, ct);
            ApplicationOperation.RequireRule(batch.BatchStatus != BatchStatus.Completed);
            var recipe = ApplicationOperation.Require(await recipes.GetByCodeAsync(batch.RecipeCode, ct));
            await command(batch, recipe, actor, ct);
            await batches.UpdateAsync(batch, ct);
            // Build the response after staging, before commit; failure still rolls back the command.
            return View(batch, recipe, await batches.GetEvidenceAsync(batch, ct));
        }, cancellationToken);

    private async Task<Batch> LoadAsync(string number, CancellationToken ct) => ApplicationOperation.Require(await batches.GetExecutionAggregateAsync(Number(number), ct));
    private static BatchNumber Number(string number) => new(ApplicationOperation.Text(number, 40));
    private static void CanonicalId(string id)
    {
        if (!Guid.TryParseExact(id, "D", out var guid) || id != guid.ToString("D")) throw new ArgumentException("A canonical technical identifier is required.");
    }
    private static BatchStepExecution Execution(Batch batch, string id)
    {
        CanonicalId(id);
        return ApplicationOperation.Require(batch.StepExecutions.SingleOrDefault(item => item.BatchStepExecutionId.Value == id));
    }
    private static void RequireVerificationPhase(Batch batch, VerificationResult result)
    {
        if (!Enum.IsDefined(result)) throw new ArgumentOutOfRangeException(nameof(result));
        ApplicationOperation.RequireRule(batch.BatchStatus is BatchStatus.Created or BatchStatus.Ready);
    }
    private static BatchView View(Batch batch, Recipe recipe, BatchEvidence evidence) => new(batch.BatchNumber.Value, batch.ProductionOrderNumber.Value,
        batch.RecipeCode.Value, batch.PlannedQuantity, batch.BatchStatus,
        new(recipe.RecipeCode.Value, recipe.Name, recipe.Version,
            recipe.Steps.Select(step => new RecipeStepView(step.RecipeStepId.Value, step.StepNumber, step.Name, step.Instructions, step.RequiresSignature)).ToArray(),
            recipe.MaterialRequirements.Select(item => new MaterialRequirementView(item.MaterialCode.Value, item.RequiredQuantity)).ToArray(),
            recipe.EquipmentRequirements.Select(item => item.EquipmentCode.Value).ToArray()),
        batch.StepExecutions.Select(item => new StepView(item.BatchStepExecutionId.Value, item.StepNumber, item.StepExecutionStatus, item.RequiresSignature,
            evidence.RecordedData.GetValueOrDefault(item.BatchStepExecutionId.Value))).ToArray(),
        batch.Deviations.Select(item => new DeviationView(item.DeviationCode.Value, item.BatchStepExecutionId?.Value, item.Description, item.DeviationStatus, item.ReviewComments, item.FinalDisposition)).ToArray(),
        evidence.Materials.Select(item => new VerificationView(item.MaterialCode.Value, item.VerificationResult, item.OperatorId.Value, item.VerifiedAt)).ToArray(),
        evidence.Equipment.Select(item => new VerificationView(item.EquipmentCode.Value, item.VerificationResult, item.OperatorId.Value, item.VerifiedAt)).ToArray(),
        evidence.Signatures.Select(item => new SignatureView(item.ElectronicSignatureId.Value, item.RelatedRecordId.Value, item.OperatorId.Value, item.Meaning, item.SignedAt)).ToArray());

    internal static AuditView AuditView(AuditEvent item) => new(item.AuditEventId.Value, item.OperatorId.Value, item.Action,
        item.AffectedEntity, item.AffectedEntityId, item.OccurredAt, item.ResultingState, item.Details);
}
