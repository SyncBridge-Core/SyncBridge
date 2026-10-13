using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Assembly;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Repositories;

internal sealed class BatchRepository(SyncBridgeDbContext context) : IBatchRepository
{
    public async Task<Batch?> GetExecutionAggregateAsync(
        BatchNumber batchNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batchNumber);
        var batchRow = await context.Batches.AsNoTracking().SingleOrDefaultAsync(
            row => row.BatchNumber == batchNumber.Value,
            cancellationToken);
        if (batchRow is null)
        {
            return null;
        }

        var orderRow = await context.ProductionOrders.AsNoTracking().SingleOrDefaultAsync(
            row => row.OrderId == batchRow.OrderId,
            cancellationToken) ?? throw new PersistenceIntegrityException("The Batch references a missing ProductionOrder.");
        var recipeRow = await context.Recipes.AsNoTracking().SingleOrDefaultAsync(
            row => row.RecipeId == orderRow.RecipeId,
            cancellationToken) ?? throw new PersistenceIntegrityException("The ProductionOrder references a missing Recipe.");
        var recipeSteps = await context.RecipeSteps.AsNoTracking()
            .Where(row => row.RecipeId == recipeRow.RecipeId)
            .ToArrayAsync(cancellationToken);
        var executions = await context.BatchStepExecutions.AsNoTracking()
            .Where(row => row.BatchId == batchRow.BatchId)
            .OrderBy(row => row.StepNumber)
            .ToArrayAsync(cancellationToken);
        var materialVerifications = await context.MaterialVerifications.AsNoTracking()
            .Where(row => row.BatchId == batchRow.BatchId)
            .ToArrayAsync(cancellationToken);
        var equipmentVerifications = await context.EquipmentVerifications.AsNoTracking()
            .Where(row => row.BatchId == batchRow.BatchId)
            .ToArrayAsync(cancellationToken);
        var deviations = await context.Deviations.AsNoTracking()
            .Where(row => row.BatchId == batchRow.BatchId)
            .ToArrayAsync(cancellationToken);
        var signatures = await context.ElectronicSignatures.AsNoTracking()
            .Where(row => row.BatchId == batchRow.BatchId)
            .ToArrayAsync(cancellationToken);
        var auditEvents = await context.AuditEvents.AsNoTracking()
            .Where(row => row.BatchId == batchRow.BatchId)
            .OrderBy(row => row.OccurredAt)
            .ToArrayAsync(cancellationToken);

        var materialIds = materialVerifications.Select(row => row.MaterialId).Distinct().ToArray();
        var equipmentIds = equipmentVerifications.Select(row => row.EquipmentId).Distinct().ToArray();
        var materials = await context.Materials.AsNoTracking()
            .Where(row => materialIds.Contains(row.MaterialId))
            .ToDictionaryAsync(row => row.MaterialId, cancellationToken);
        var equipment = await context.Equipment.AsNoTracking()
            .Where(row => equipmentIds.Contains(row.EquipmentId))
            .ToDictionaryAsync(row => row.EquipmentId, cancellationToken);
        var referencedUsers = auditEvents.Select(row => row.UserId)
            .Concat(executions.Where(row => row.CompletedByUserId != null).Select(row => row.CompletedByUserId!))
            .Concat(materialVerifications.Select(row => row.VerifiedByUserId))
            .Concat(equipmentVerifications.Select(row => row.VerifiedByUserId))
            .Concat(deviations.Select(row => row.ReportedByUserId))
            .Concat(signatures.Select(row => row.UserId))
            .Distinct()
            .ToArray();
        var userIds = (await context.AppUsers.AsNoTracking()
            .Where(row => referencedUsers.Contains(row.UserId))
            .Select(row => row.UserId)
            .ToArrayAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        return BatchAggregateAssembler.Assemble(
            batchRow,
            orderRow,
            recipeRow,
            recipeSteps,
            executions,
            materialVerifications,
            equipmentVerifications,
            deviations,
            signatures,
            auditEvents,
            materials,
            equipment,
            userIds);
    }

    public async Task AddAsync(Batch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var orderRow = await context.ProductionOrders.SingleOrDefaultAsync(
            row => row.OrderNumber == batch.ProductionOrderNumber.Value,
            cancellationToken) ?? throw new PersistenceIntegrityException("The Batch ProductionOrder does not exist.");
        var recipeRow = await context.Recipes.SingleOrDefaultAsync(
            row => row.RecipeId == orderRow.RecipeId && row.RecipeCode == batch.RecipeCode.Value,
            cancellationToken) ?? throw new PersistenceIntegrityException("The Batch Recipe association is invalid.");

        if (batch.PlannedQuantity != orderRow.Quantity)
        {
            throw new PersistenceIntegrityException("The Batch quantity differs from its ProductionOrder quantity.");
        }

        var batchId = Guid.NewGuid();
        var created = RequireSingleAudit(batch.AuditEvents, AuditEventType.BatchCreated);
        var started = FindOptionalSingleAudit(batch.AuditEvents, AuditEventType.BatchProcessingStarted);
        var completed = FindOptionalSingleAudit(batch.AuditEvents, AuditEventType.BatchCompleted);
        await context.Batches.AddAsync(new BatchRow
        {
            BatchId = batchId,
            BatchNumber = DbdsStringLengthValidator.RequireWithinLimit(batch.BatchNumber.Value, 40, nameof(batch)),
            OrderId = orderRow.OrderId,
            Status = batch.BatchStatus,
            CreatedAt = created.OccurredAt,
            StartedAt = started?.OccurredAt,
            CompletedAt = completed?.OccurredAt,
            FinalDisposition = null
        }, cancellationToken);

        foreach (var execution in batch.StepExecutions)
        {
            await context.BatchStepExecutions.AddAsync(new BatchStepExecutionRow
            {
                StepExecutionId = TechnicalGuidConverter.ToGuid(execution.BatchStepExecutionId.Value),
                BatchId = batchId,
                RecipeStepId = TechnicalGuidConverter.ToGuid(execution.RecipeStepId.Value),
                StepNumber = execution.StepNumber,
                Status = execution.StepExecutionStatus,
                StartedAt = execution.StartedAt,
                CompletedAt = execution.CompletedAt,
                CompletedByUserId = execution.CompletedBy?.Value,
                RecordedData = null
            }, cancellationToken);
        }

        foreach (var deviation in batch.Deviations)
        {
            await context.Deviations.AddAsync(new DeviationRow
            {
                DeviationId = Guid.NewGuid(),
                DeviationCode = DbdsStringLengthValidator.RequireWithinLimit(deviation.DeviationCode.Value, 40, nameof(batch)),
                BatchId = batchId,
                StepExecutionId = deviation.BatchStepExecutionId is null
                    ? null
                    : TechnicalGuidConverter.ToGuid(deviation.BatchStepExecutionId.Value),
                Description = DbdsStringLengthValidator.RequireWithinLimit(deviation.Description, 2000, nameof(batch)),
                Status = deviation.DeviationStatus,
                ReportedByUserId = deviation.ReportedBy.Value,
                OpenedAt = deviation.OpenedAt,
                ReviewComments = deviation.ReviewComments,
                FinalDisposition = deviation.FinalDisposition,
                ResolvedAt = deviation.ResolvedAt
            }, cancellationToken);
        }

        foreach (var auditEvent in batch.AuditEvents)
        {
            await context.AuditEvents.AddAsync(new AuditEventRow
            {
                AuditEventId = TechnicalGuidConverter.ToGuid(auditEvent.AuditEventId.Value),
                BatchId = batchId,
                UserId = DbdsStringLengthValidator.RequireWithinLimit(auditEvent.OperatorId.Value, 64, nameof(batch)),
                Action = auditEvent.Action,
                AffectedEntity = DbdsStringLengthValidator.RequireWithinLimit(auditEvent.AffectedEntity, 60, nameof(batch)),
                AffectedEntityId = DbdsStringLengthValidator.RequireWithinLimit(auditEvent.AffectedEntityId, 256, nameof(batch)),
                OccurredAt = auditEvent.OccurredAt,
                ResultingState = auditEvent.ResultingState,
                Details = auditEvent.Details
            }, cancellationToken);
        }

        _ = recipeRow;
    }

    private static AuditEvent RequireSingleAudit(IEnumerable<AuditEvent> auditEvents, AuditEventType action)
    {
        var matches = auditEvents.Where(auditEvent => auditEvent.Action == action).ToArray();
        return matches.Length == 1
            ? matches[0]
            : throw new PersistenceIntegrityException($"The Batch requires exactly one {action} event.");
    }

    private static AuditEvent? FindOptionalSingleAudit(IEnumerable<AuditEvent> auditEvents, AuditEventType action)
    {
        var matches = auditEvents.Where(auditEvent => auditEvent.Action == action).ToArray();
        return matches.Length switch
        {
            0 => null,
            1 => matches[0],
            _ => throw new PersistenceIntegrityException($"The Batch has ambiguous {action} evidence.")
        };
    }
}
