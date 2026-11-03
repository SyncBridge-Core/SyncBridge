using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Reporting;
using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Application.Services;

/// <summary>Read-only UC-005 boundary. Identity and completion are checked before any renderer call.</summary>
public sealed class BatchReportService(ApplicationOperation operations, IReportBatchReader batches, IBatchReportRenderer renderer)
{
    public Task<OperationResult<ReportDocument>> GenerateAsync(string batchNumber, ReportFormat format = ReportFormat.Browser,
        ReportContent content = ReportContent.ElectronicBatchRecord, CancellationToken cancellationToken = default) =>
        operations.RunAsync<ReportDocument>([ManufacturingRoles.Supervisor, ManufacturingRoles.Quality], false, async (_, ct) =>
        {
            if (!Enum.IsDefined(format) || !Enum.IsDefined(content)) throw new ArgumentException("Invalid report option.");
            var batch = ApplicationOperation.Require(await batches.FindAsync(ApplicationOperation.Text(batchNumber, 40), ct));
            ApplicationOperation.RequireRule(batch.Status == BatchStatus.Completed);
            return await renderer.RenderAsync(batch, format, content, ct);
        }, cancellationToken);
}
