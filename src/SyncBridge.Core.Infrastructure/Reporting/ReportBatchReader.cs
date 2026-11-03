using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Reporting;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Infrastructure.Reporting;

internal sealed class ReportBatchReader(SyncBridgeDbContext context) : IReportBatchReader
{
    public Task<ReportBatch?> FindAsync(string batchNumber, CancellationToken cancellationToken) =>
        context.Batches.AsNoTracking().Where(row => row.BatchNumber == batchNumber)
            .Select(row => new ReportBatch(row.BatchId, row.BatchNumber, row.Status)).SingleOrDefaultAsync(cancellationToken);
}
