using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Assembly;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence.Repositories;

internal sealed partial class BatchRepository
{
    public async Task UpdateAsync(Batch batch, CancellationToken cancellationToken = default)
    {
        var row = await BatchRowAsync(batch.BatchNumber, cancellationToken);
        if (row.Status == BatchStatus.Completed) throw new PersistenceIntegrityException("Completed records cannot be changed.");
        row.Status = batch.BatchStatus;
        row.StartedAt = batch.AuditEvents.SingleOrDefault(a => a.Action == AuditEventType.BatchProcessingStarted)?.OccurredAt;
        row.CompletedAt = batch.AuditEvents.SingleOrDefault(a => a.Action == AuditEventType.BatchCompleted)?.OccurredAt;

        foreach (var execution in batch.StepExecutions)
        {
            var id = TechnicalGuidConverter.ToGuid(execution.BatchStepExecutionId.Value);
            var stored = await context.BatchStepExecutions.SingleOrDefaultAsync(item => item.StepExecutionId == id, cancellationToken);
            if (stored is null)
            {
                stored = new BatchStepExecutionRow { StepExecutionId = id, BatchId = row.BatchId,
                    RecipeStepId = TechnicalGuidConverter.ToGuid(execution.RecipeStepId.Value), StepNumber = execution.StepNumber };
                await context.BatchStepExecutions.AddAsync(stored, cancellationToken);
            }
            if (stored.BatchId != row.BatchId) throw new PersistenceIntegrityException("Execution belongs to another Batch.");
            stored.Status = execution.StepExecutionStatus;
            stored.StartedAt = execution.StartedAt;
            stored.CompletedAt = execution.CompletedAt;
            stored.CompletedByUserId = execution.CompletedBy?.Value;
            // RecordedData is unrelated to these transitions and is preserved.
        }
        foreach (var deviation in batch.Deviations)
        {
            var stored = await context.Deviations.SingleOrDefaultAsync(item => item.DeviationCode == deviation.DeviationCode.Value, cancellationToken);
            if (stored is null)
            {
                stored = new DeviationRow { DeviationId = Guid.NewGuid(), DeviationCode = deviation.DeviationCode.Value,
                    BatchId = row.BatchId, StepExecutionId = deviation.BatchStepExecutionId is null ? null : TechnicalGuidConverter.ToGuid(deviation.BatchStepExecutionId.Value),
                    Description = deviation.Description, ReportedByUserId = deviation.ReportedBy.Value, OpenedAt = deviation.OpenedAt };
                await context.Deviations.AddAsync(stored, cancellationToken);
            }
            if (stored.BatchId != row.BatchId) throw new PersistenceIntegrityException("Deviation belongs to another Batch.");
            stored.Status = deviation.DeviationStatus;
            stored.ReviewComments = deviation.ReviewComments;
            stored.FinalDisposition = deviation.FinalDisposition;
            stored.ResolvedAt = deviation.ResolvedAt;
        }
        foreach (var audit in batch.AuditEvents) await AuditPersistence.AppendAsync(context, audit, row.BatchId, cancellationToken);
    }

    public async Task SaveMaterialVerificationAsync(MaterialVerification verification, CancellationToken cancellationToken = default)
    {
        var batch = await BatchRowAsync(verification.BatchNumber, cancellationToken);
        var material = await context.Materials.SingleAsync(row => row.MaterialCode == verification.MaterialCode.Value, cancellationToken);
        var row = await context.MaterialVerifications.SingleOrDefaultAsync(item => item.BatchId == batch.BatchId && item.MaterialId == material.MaterialId, cancellationToken);
        if (row is null)
        {
            row = new MaterialVerificationRow { VerificationId = Guid.NewGuid(), BatchId = batch.BatchId, MaterialId = material.MaterialId };
            await context.MaterialVerifications.AddAsync(row, cancellationToken);
        }
        row.IsAccepted = verification.VerificationResult == VerificationResult.Passed;
        row.VerifiedByUserId = verification.OperatorId.Value;
        row.VerifiedAt = verification.VerifiedAt;
        await AuditPersistence.AppendAsync(context, verification.AuditEvidence, batch.BatchId, cancellationToken);
    }

    public async Task SaveEquipmentVerificationAsync(EquipmentVerification verification, CancellationToken cancellationToken = default)
    {
        var batch = await BatchRowAsync(verification.BatchNumber, cancellationToken);
        var equipment = await context.Equipment.SingleAsync(row => row.EquipmentCode == verification.EquipmentCode.Value, cancellationToken);
        var row = await context.EquipmentVerifications.SingleOrDefaultAsync(item => item.BatchId == batch.BatchId && item.EquipmentId == equipment.EquipmentId, cancellationToken);
        if (row is null)
        {
            row = new EquipmentVerificationRow { VerificationId = Guid.NewGuid(), BatchId = batch.BatchId, EquipmentId = equipment.EquipmentId };
            await context.EquipmentVerifications.AddAsync(row, cancellationToken);
        }
        row.IsReady = verification.VerificationResult == VerificationResult.Passed;
        row.VerifiedByUserId = verification.OperatorId.Value;
        row.VerifiedAt = verification.VerifiedAt;
        await AuditPersistence.AppendAsync(context, verification.AuditEvidence, batch.BatchId, cancellationToken);
    }

    public async Task SaveSignatureAsync(ElectronicSignature signature, CancellationToken cancellationToken = default)
    {
        var batch = await BatchRowAsync(signature.BatchNumber, cancellationToken);
        var target = TechnicalGuidConverter.ToGuid(signature.RelatedRecordId.Value);
        if (signature.RelatedRecordType != RelatedRecordType.BatchStepExecution ||
            !await context.BatchStepExecutions.AnyAsync(row => row.StepExecutionId == target && row.BatchId == batch.BatchId, cancellationToken))
            throw new PersistenceIntegrityException("Signature target is outside the Batch.");
        await context.ElectronicSignatures.AddAsync(new ElectronicSignatureRow
        {
            SignatureId = TechnicalGuidConverter.ToGuid(signature.ElectronicSignatureId.Value), BatchId = batch.BatchId,
            RelatedRecordType = signature.RelatedRecordType.Value, RelatedRecordId = target,
            UserId = signature.OperatorId.Value, Meaning = signature.Meaning, SignedAt = signature.SignedAt
        }, cancellationToken);
        await AuditPersistence.AppendAsync(context, signature.AuditEvidence, batch.BatchId, cancellationToken);
    }

    public async Task RecordStepDataAsync(BatchStepExecutionId executionId, string data, CancellationToken cancellationToken = default)
    {
        var id = TechnicalGuidConverter.ToGuid(executionId.Value);
        var row = await context.BatchStepExecutions.SingleAsync(item => item.StepExecutionId == id, cancellationToken);
        row.RecordedData = data;
    }

    public async Task<BatchEvidence> GetEvidenceAsync(Batch batch, CancellationToken cancellationToken = default)
    {
        var row = await BatchRowAsync(batch.BatchNumber, cancellationToken);
        var materials = await context.MaterialVerifications.Where(item => item.BatchId == row.BatchId).ToListAsync(cancellationToken);
        materials.AddRange(context.MaterialVerifications.Local.Where(item => item.BatchId == row.BatchId && !materials.Contains(item)));
        var equipment = await context.EquipmentVerifications.Where(item => item.BatchId == row.BatchId).ToListAsync(cancellationToken);
        equipment.AddRange(context.EquipmentVerifications.Local.Where(item => item.BatchId == row.BatchId && !equipment.Contains(item)));
        var signatures = await context.ElectronicSignatures.Where(item => item.BatchId == row.BatchId).ToListAsync(cancellationToken);
        signatures.AddRange(context.ElectronicSignatures.Local.Where(item => item.BatchId == row.BatchId && !signatures.Contains(item)));
        var executions = await context.BatchStepExecutions.Where(item => item.BatchId == row.BatchId).ToListAsync(cancellationToken);
        executions.AddRange(context.BatchStepExecutions.Local.Where(item => item.BatchId == row.BatchId && !executions.Contains(item)));
        var materialIds = materials.Select(item => item.MaterialId).ToArray();
        var equipmentIds = equipment.Select(item => item.EquipmentId).ToArray();
        var materialRows = await context.Materials.AsNoTracking().Where(item => materialIds.Contains(item.MaterialId)).ToDictionaryAsync(item => item.MaterialId, cancellationToken);
        var equipmentRows = await context.Equipment.AsNoTracking().Where(item => equipmentIds.Contains(item.EquipmentId)).ToDictionaryAsync(item => item.EquipmentId, cancellationToken);
        var referencedUsers = materials.Select(item => item.VerifiedByUserId).Concat(equipment.Select(item => item.VerifiedByUserId)).Concat(signatures.Select(item => item.UserId)).Distinct().ToArray();
        var users = (await context.AppUsers.AsNoTracking().Where(item => referencedUsers.Contains(item.UserId)).Select(item => item.UserId).ToArrayAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
        var audits = batch.AuditEvents.Concat(context.AuditEvents.Local.Where(item => item.BatchId == row.BatchId).Select(item => AuditPersistence.Read(item, batch.BatchNumber)))
            .DistinctBy(item => item.AuditEventId).ToArray();
        return new(BatchAggregateAssembler.ValidateMaterialVerifications(materials, row, batch.BatchNumber, audits, materialRows, users),
            BatchAggregateAssembler.ValidateEquipmentVerifications(equipment, row, batch.BatchNumber, audits, equipmentRows, users),
            BatchAggregateAssembler.ValidateSignatures(signatures, row, batch.BatchNumber, batch.StepExecutions, audits, users),
            executions.ToDictionary(item => TechnicalGuidConverter.ToCanonicalString(item.StepExecutionId), item => item.RecordedData));
    }

    public async Task<IReadOnlyList<BatchNumber>> GetReviewCandidatesAsync(CancellationToken cancellationToken = default) =>
        (await context.Batches.AsNoTracking().Where(row => row.Status != BatchStatus.Completed).OrderBy(row => row.BatchNumber)
            .Select(row => row.BatchNumber).ToArrayAsync(cancellationToken)).Select(value => new BatchNumber(value)).ToArray();

    private Task<BatchRow> BatchRowAsync(BatchNumber number, CancellationToken ct) =>
        context.Batches.SingleAsync(row => row.BatchNumber == number.Value, ct);
}
