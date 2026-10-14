using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Assembly;

internal static class BatchAggregateAssembler
{
    internal static Batch Assemble(
        BatchRow batchRow,
        ProductionOrderRow orderRow,
        RecipeRow recipeRow,
        IEnumerable<RecipeStepRow> recipeStepRows,
        IEnumerable<BatchStepExecutionRow> executionRows,
        IEnumerable<MaterialVerificationRow> materialVerificationRows,
        IEnumerable<EquipmentVerificationRow> equipmentVerificationRows,
        IEnumerable<DeviationRow> deviationRows,
        IEnumerable<ElectronicSignatureRow> signatureRows,
        IEnumerable<AuditEventRow> auditRows,
        IReadOnlyDictionary<Guid, MaterialRow> materials,
        IReadOnlyDictionary<Guid, EquipmentRow> equipment,
        IReadOnlySet<string> userIds)
    {
        ArgumentNullException.ThrowIfNull(batchRow);
        ArgumentNullException.ThrowIfNull(orderRow);
        ArgumentNullException.ThrowIfNull(recipeRow);

        if (batchRow.OrderId != orderRow.OrderId || orderRow.RecipeId != recipeRow.RecipeId)
        {
            throw new PersistenceIntegrityException("The Batch, ProductionOrder, and Recipe relationship is inconsistent.");
        }

        var batchNumber = new BatchNumber(
            DbdsStringLengthValidator.RequireWithinLimit(batchRow.BatchNumber, 40, nameof(batchRow.BatchNumber)));
        var productionOrder = ProductionOrderAssembler.Assemble(orderRow);
        var recipeCode = new RecipeCode(
            DbdsStringLengthValidator.RequireWithinLimit(recipeRow.RecipeCode, 40, nameof(recipeRow.RecipeCode)));

        var canonicalAuditEvents = CreateAuditEvents(batchRow, batchNumber, auditRows, userIds);
        ValidateAuthoritativeTimestamps(batchRow, canonicalAuditEvents.Values);

        var recipeSteps = recipeStepRows.ToDictionary(row => row.RecipeStepId);
        var executions = executionRows
            .OrderBy(row => row.StepNumber)
            .Select(row => RehydrateExecution(row, batchRow, batchNumber, recipeSteps, userIds))
            .ToArray();

        var deviations = deviationRows
            .Select(row => RehydrateDeviation(row, batchRow, batchNumber, canonicalAuditEvents.Values, userIds))
            .ToArray();

        var deviationAuditIds = deviations
            .SelectMany(deviation => deviation.AuditEvents)
            .Select(auditEvent => auditEvent.AuditEventId)
            .ToHashSet();

        ValidateMaterialVerifications(
            materialVerificationRows,
            batchRow,
            batchNumber,
            canonicalAuditEvents.Values,
            materials,
            userIds);
        ValidateEquipmentVerifications(
            equipmentVerificationRows,
            batchRow,
            batchNumber,
            canonicalAuditEvents.Values,
            equipment,
            userIds);
        ValidateSignatures(
            signatureRows,
            batchRow,
            batchNumber,
            executions,
            canonicalAuditEvents.Values,
            userIds);

        var directAuditEvents = canonicalAuditEvents.Values
            .Where(auditEvent => !deviationAuditIds.Contains(auditEvent.AuditEventId))
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .ToArray();

        return Batch.Rehydrate(
            batchNumber,
            productionOrder,
            recipeCode,
            batchRow.Status,
            executions,
            deviations,
            directAuditEvents);
    }

    private static Dictionary<Guid, AuditEvent> CreateAuditEvents(
        BatchRow batchRow,
        BatchNumber batchNumber,
        IEnumerable<AuditEventRow> auditRows,
        IReadOnlySet<string> userIds)
    {
        var result = new Dictionary<Guid, AuditEvent>();
        foreach (var row in auditRows)
        {
            if (row.BatchId != batchRow.BatchId)
            {
                throw new PersistenceIntegrityException("An AuditEvent has the wrong Batch association.");
            }

            EnsureKnownUser(row.UserId, userIds);
            if (!result.TryAdd(
                    row.AuditEventId,
                    new AuditEvent(
                        new AuditEventId(TechnicalGuidConverter.ToCanonicalString(row.AuditEventId)),
                        batchNumber,
                        new OperatorId(DbdsStringLengthValidator.RequireWithinLimit(row.UserId, 64, nameof(row.UserId))),
                        row.Action,
                        DbdsStringLengthValidator.RequireWithinLimit(row.AffectedEntity, 60, nameof(row.AffectedEntity)),
                        DbdsStringLengthValidator.RequireWithinLimit(row.AffectedEntityId, 256, nameof(row.AffectedEntityId)),
                        row.OccurredAt,
                        row.ResultingState,
                        row.Details)))
            {
                throw new PersistenceIntegrityException("Duplicate AuditEvent identity detected.");
            }
        }

        return result;
    }

    private static BatchStepExecution RehydrateExecution(
        BatchStepExecutionRow row,
        BatchRow batchRow,
        BatchNumber batchNumber,
        IReadOnlyDictionary<Guid, RecipeStepRow> recipeSteps,
        IReadOnlySet<string> userIds)
    {
        if (row.BatchId != batchRow.BatchId || !recipeSteps.TryGetValue(row.RecipeStepId, out var recipeStep))
        {
            throw new PersistenceIntegrityException("A BatchStepExecution association is invalid.");
        }

        if (row.CompletedByUserId is not null)
        {
            EnsureKnownUser(row.CompletedByUserId, userIds);
        }

        return BatchStepExecution.Rehydrate(
            new BatchStepExecutionId(TechnicalGuidConverter.ToCanonicalString(row.StepExecutionId)),
            batchNumber,
            new RecipeStepId(TechnicalGuidConverter.ToCanonicalString(row.RecipeStepId)),
            row.StepNumber,
            recipeStep.RequiresSignature,
            row.Status,
            row.StartedAt,
            row.CompletedAt,
            row.CompletedByUserId is null ? null : new OperatorId(row.CompletedByUserId));
    }

    private static Deviation RehydrateDeviation(
        DeviationRow row,
        BatchRow batchRow,
        BatchNumber batchNumber,
        IEnumerable<AuditEvent> auditEvents,
        IReadOnlySet<string> userIds)
    {
        if (row.BatchId != batchRow.BatchId)
        {
            throw new PersistenceIntegrityException("A Deviation has the wrong Batch association.");
        }

        EnsureKnownUser(row.ReportedByUserId, userIds);
        var ownedEvents = auditEvents
            .Where(auditEvent =>
                auditEvent.AffectedEntity == nameof(Deviation) &&
                auditEvent.AffectedEntityId == row.DeviationCode)
            .OrderBy(auditEvent => auditEvent.OccurredAt)
            .ToArray();

        if (ownedEvents.Length == 0)
        {
            throw new PersistenceIntegrityException("A Deviation is missing its audit history.");
        }

        return Deviation.Rehydrate(
            new DeviationCode(DbdsStringLengthValidator.RequireWithinLimit(row.DeviationCode, 40, nameof(row.DeviationCode))),
            batchNumber,
            row.StepExecutionId is null
                ? null
                : new BatchStepExecutionId(TechnicalGuidConverter.ToCanonicalString(row.StepExecutionId.Value)),
            DbdsStringLengthValidator.RequireWithinLimit(row.Description, 2000, nameof(row.Description)),
            new OperatorId(row.ReportedByUserId),
            row.OpenedAt,
            row.Status,
            row.ReviewComments,
            row.FinalDisposition,
            row.ResolvedAt,
            ownedEvents);
    }

    internal static IReadOnlyList<MaterialVerification> ValidateMaterialVerifications(
        IEnumerable<MaterialVerificationRow> rows,
        BatchRow batchRow,
        BatchNumber batchNumber,
        IEnumerable<AuditEvent> auditEvents,
        IReadOnlyDictionary<Guid, MaterialRow> materials,
        IReadOnlySet<string> userIds)
    {
        var restored = new List<MaterialVerification>();
        foreach (var row in rows)
        {
            if (row.BatchId != batchRow.BatchId || !materials.TryGetValue(row.MaterialId, out var material))
            {
                throw new PersistenceIntegrityException("A MaterialVerification association is invalid.");
            }

            EnsureKnownUser(row.VerifiedByUserId, userIds);
            var result = row.IsAccepted ? VerificationResult.Passed : VerificationResult.Failed;
            var affectedId = $"{batchNumber.Value}:{material.MaterialCode}:{row.VerifiedByUserId}:{row.VerifiedAt:O}";
            var evidence = FindSingleEvidence(auditEvents, AuditEventType.MaterialVerified, affectedId);

            restored.Add(MaterialVerification.Rehydrate(
                batchNumber,
                new MaterialCode(material.MaterialCode),
                new OperatorId(row.VerifiedByUserId),
                row.VerifiedAt,
                result,
                evidence));
        }
        return restored;
    }

    internal static IReadOnlyList<EquipmentVerification> ValidateEquipmentVerifications(
        IEnumerable<EquipmentVerificationRow> rows,
        BatchRow batchRow,
        BatchNumber batchNumber,
        IEnumerable<AuditEvent> auditEvents,
        IReadOnlyDictionary<Guid, EquipmentRow> equipment,
        IReadOnlySet<string> userIds)
    {
        var restored = new List<EquipmentVerification>();
        foreach (var row in rows)
        {
            if (row.BatchId != batchRow.BatchId || !equipment.TryGetValue(row.EquipmentId, out var equipmentRow))
            {
                throw new PersistenceIntegrityException("An EquipmentVerification association is invalid.");
            }

            EnsureKnownUser(row.VerifiedByUserId, userIds);
            var result = row.IsReady ? VerificationResult.Passed : VerificationResult.Failed;
            var affectedId = $"{batchNumber.Value}:{equipmentRow.EquipmentCode}:{row.VerifiedByUserId}:{row.VerifiedAt:O}";
            var evidence = FindSingleEvidence(auditEvents, AuditEventType.EquipmentVerified, affectedId);

            restored.Add(EquipmentVerification.Rehydrate(
                batchNumber,
                new EquipmentCode(equipmentRow.EquipmentCode),
                new OperatorId(row.VerifiedByUserId),
                row.VerifiedAt,
                result,
                evidence));
        }
        return restored;
    }

    internal static IReadOnlyList<ElectronicSignature> ValidateSignatures(
        IEnumerable<ElectronicSignatureRow> rows,
        BatchRow batchRow,
        BatchNumber batchNumber,
        IReadOnlyCollection<BatchStepExecution> executions,
        IEnumerable<AuditEvent> auditEvents,
        IReadOnlySet<string> userIds)
    {
        var restored = new List<ElectronicSignature>();
        foreach (var row in rows)
        {
            if (row.BatchId != batchRow.BatchId || row.RelatedRecordType != RelatedRecordType.BatchStepExecution.Value)
            {
                throw new PersistenceIntegrityException("An ElectronicSignature association is invalid.");
            }

            EnsureKnownUser(row.UserId, userIds);
            var relatedId = TechnicalGuidConverter.ToCanonicalString(row.RelatedRecordId);
            if (!executions.Any(execution => execution.BatchStepExecutionId.Value == relatedId))
            {
                throw new PersistenceIntegrityException("An ElectronicSignature targets a missing execution in the Batch.");
            }

            var signatureId = TechnicalGuidConverter.ToCanonicalString(row.SignatureId);
            var evidence = FindSingleEvidence(auditEvents, AuditEventType.ElectronicSignatureApplied, signatureId);
            restored.Add(ElectronicSignature.Rehydrate(
                new ElectronicSignatureId(signatureId),
                batchNumber,
                RelatedRecordType.BatchStepExecution,
                new RelatedRecordId(relatedId),
                new OperatorId(row.UserId),
                row.Meaning,
                row.SignedAt,
                evidence));
        }
        return restored;
    }

    private static AuditEvent FindSingleEvidence(
        IEnumerable<AuditEvent> auditEvents,
        AuditEventType action,
        string affectedEntityId)
    {
        var matches = auditEvents
            .Where(auditEvent => auditEvent.Action == action && auditEvent.AffectedEntityId == affectedEntityId)
            .ToArray();

        return matches.Length == 1
            ? matches[0]
            : throw new PersistenceIntegrityException("Required historical evidence is missing or ambiguous.");
    }

    private static void ValidateAuthoritativeTimestamps(
        BatchRow batchRow,
        IEnumerable<AuditEvent> auditEvents)
    {
        ValidateTimestamp(auditEvents, AuditEventType.BatchCreated, batchRow.CreatedAt, true);
        ValidateTimestamp(auditEvents, AuditEventType.BatchProcessingStarted, batchRow.StartedAt, false);
        ValidateTimestamp(auditEvents, AuditEventType.BatchCompleted, batchRow.CompletedAt, false);
    }

    private static void ValidateTimestamp(
        IEnumerable<AuditEvent> auditEvents,
        AuditEventType action,
        DateTimeOffset? expected,
        bool required)
    {
        var matches = auditEvents.Where(auditEvent => auditEvent.Action == action).ToArray();
        if ((expected is null && matches.Length != 0) ||
            (expected is not null && (matches.Length != 1 || matches[0].OccurredAt != expected)) ||
            (required && matches.Length != 1))
        {
            throw new PersistenceIntegrityException($"Authoritative {action} timestamp evidence is inconsistent.");
        }
    }

    private static void EnsureKnownUser(string userId, IReadOnlySet<string> userIds)
    {
        DbdsStringLengthValidator.RequireWithinLimit(userId, 64, nameof(userId));
        if (!userIds.Contains(userId))
        {
            throw new PersistenceIntegrityException($"Referenced AppUser '{userId}' does not exist.");
        }
    }
}
