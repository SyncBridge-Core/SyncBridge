using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Contracts.Reporting;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages.Batches;

public sealed class ReportModel(BatchReportService reports) : ManufacturingPageModel
{
    public async Task<IActionResult> OnGetAsync(string number, ReportFormat format = ReportFormat.Browser,
        ReportContent content = ReportContent.ElectronicBatchRecord)
    {
        if (!ModelState.IsValid) return Failure(OperationOutcome.InvalidInput);
        var result = await reports.GenerateAsync(number, format, content, HttpContext.RequestAborted);
        if (!result.Succeeded) return Failure(result.Outcome);
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        // Native HTML is a passive document: no script, external navigation/resources or form submission.
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; img-src data:; frame-ancestors 'self'; sandbox";
        return format == ReportFormat.Pdf
            ? File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
            : File(result.Value!.Content, result.Value.ContentType);
    }
}
