using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;

namespace SyncBridge.Core.Infrastructure.Persistence;

internal sealed class ApplicationUnitOfWork(SyncBridgeDbContext context) : IApplicationUnitOfWork
{
    private IDbContextTransaction? commandTransaction;

    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (commandTransaction is not null || context.Database.CurrentTransaction is not null || context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("A controlled command requires a clean persistence scope.");
        context.ChangeTracker.Clear();
        commandTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (commandTransaction is not null) await commandTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (commandTransaction is not null) await commandTransaction.DisposeAsync();
            commandTransaction = null;
            context.ChangeTracker.Clear();
        }
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        RejectEvidenceMutation();
        RejectCompletedBatchMutation();
        await RejectCompletedEvidenceMutationAsync(cancellationToken);
        var count = await context.SaveChangesAsync(cancellationToken);
        if (commandTransaction is not null)
        {
            await commandTransaction.CommitAsync(cancellationToken);
            await commandTransaction.DisposeAsync();
            commandTransaction = null;
            context.ChangeTracker.Clear();
        }
        return count;
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

    private async Task RejectCompletedEvidenceMutationAsync(CancellationToken ct)
    {
        var ids = context.ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity switch
            {
                BatchStepExecutionRow r => (Guid?)r.BatchId,
                MaterialVerificationRow r => r.BatchId,
                EquipmentVerificationRow r => r.BatchId,
                DeviationRow r => r.BatchId,
                ElectronicSignatureRow r => r.BatchId,
                _ => null
            }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        if (ids.Length > 0 && await context.Batches.AsNoTracking().AnyAsync(row => ids.Contains(row.BatchId) && row.Status == BatchStatus.Completed, ct))
            throw new PersistenceIntegrityException("Completed Batch evidence cannot be changed.");
    }
}
