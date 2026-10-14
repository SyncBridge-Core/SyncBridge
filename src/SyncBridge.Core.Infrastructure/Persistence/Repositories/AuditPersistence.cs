using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Repositories;

/// <summary>Shared row mapping for append-only evidence, including actions without a Batch.</summary>
internal static class AuditPersistence
{
    internal static async Task AppendAsync(SyncBridgeDbContext context, AuditEvent audit, Guid? batchId, CancellationToken ct)
    {
        var id = TechnicalGuidConverter.ToGuid(audit.AuditEventId.Value);
        if (context.AuditEvents.Local.Any(row => row.AuditEventId == id) || await context.AuditEvents.AnyAsync(row => row.AuditEventId == id, ct)) return;
        await context.AuditEvents.AddAsync(new AuditEventRow
        {
            AuditEventId = id, BatchId = batchId,
            UserId = DbdsStringLengthValidator.RequireWithinLimit(audit.OperatorId.Value, 64, nameof(audit.OperatorId)),
            Action = audit.Action,
            AffectedEntity = DbdsStringLengthValidator.RequireWithinLimit(audit.AffectedEntity, 60, nameof(audit.AffectedEntity)),
            AffectedEntityId = DbdsStringLengthValidator.RequireWithinLimit(audit.AffectedEntityId, 256, nameof(audit.AffectedEntityId)),
            OccurredAt = audit.OccurredAt, ResultingState = audit.ResultingState, Details = audit.Details
        }, ct);
    }

    internal static AuditEvent Read(AuditEventRow row, BatchNumber? number) => new(
        new AuditEventId(TechnicalGuidConverter.ToCanonicalString(row.AuditEventId)), number, new OperatorId(row.UserId),
        row.Action, row.AffectedEntity, row.AffectedEntityId, row.OccurredAt, row.ResultingState, row.Details);
}
