using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Application.Contracts.Reporting;

public enum ReportFormat { Browser, Pdf }
public enum ReportContent { ElectronicBatchRecord, BatchSummary, Deviations }
public sealed record ReportBatch(Guid BatchId, string BatchNumber, BatchStatus Status);
public sealed record ReportDocument(byte[] Content, string ContentType, string FileName);

/// <summary>Resolves the persisted identity without accepting a caller-supplied report parameter.</summary>
public interface IReportBatchReader
{
    Task<ReportBatch?> FindAsync(string batchNumber, CancellationToken cancellationToken);
}

/// <summary>Infrastructure renders the approved definition; callers cannot choose SQL, URLs or report paths.</summary>
public interface IBatchReportRenderer
{
    Task<ReportDocument> RenderAsync(ReportBatch batch, ReportFormat format, ReportContent content, CancellationToken cancellationToken);
}
