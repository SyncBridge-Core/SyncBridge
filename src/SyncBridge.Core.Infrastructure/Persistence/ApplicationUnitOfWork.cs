using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence;

internal sealed class ApplicationUnitOfWork(SyncBridgeDbContext context) : IApplicationUnitOfWork
{
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        RejectEvidenceMutation();
        RejectCompletedBatchMutation();
        return context.SaveChangesAsync(cancellationToken);
    }

    private void RejectEvidenceMutation()
    {
        var prohibitedEvidenceMutation = context.ChangeTracker.Entries()
            .Any(entry =>
                (entry.Entity is AuditEventRow or ElectronicSignatureRow) &&
                entry.State is EntityState.Modified or EntityState.Deleted);
        if (prohibitedEvidenceMutation)
        {
            throw new PersistenceIntegrityException("Audit events and electronic signatures are immutable.");
        }
    }

    private void RejectCompletedBatchMutation()
    {
        foreach (var entry in context.ChangeTracker.Entries<BatchRow>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted &&
                entry.OriginalValues.GetValue<BatchStatus>(nameof(BatchRow.Status)) == BatchStatus.Completed)
            {
                throw new PersistenceIntegrityException("Completed Batch records cannot be modified.");
            }
        }
    }
}
