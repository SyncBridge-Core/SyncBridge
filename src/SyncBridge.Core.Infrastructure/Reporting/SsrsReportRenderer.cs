using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using SyncBridge.Core.Application.Contracts.Reporting;

namespace SyncBridge.Core.Infrastructure.Reporting;

public sealed class SsrsReportingOptions
{
    public string ServiceUrl { get; set; } = "http://127.0.0.1/ReportServer/ReportExecution2005.asmx";
    public string ReportPath { get; set; } = "/SyncBridge/ElectronicBatchRecord";
}

/// <summary>Native execution SOAP only. SSRS owns data-source credentials, queries and HTML/PDF rendering.</summary>
public sealed class SsrsReportRenderer(HttpClient client, IOptions<SsrsReportingOptions> options) : IBatchReportRenderer
{
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Execution = "http://schemas.microsoft.com/sqlserver/2005/06/30/reporting/reportingservices";
    private const int MaximumResponseCharacters = 64 * 1024 * 1024;

    public async Task<ReportDocument> RenderAsync(ReportBatch batch, ReportFormat format, ReportContent content, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(format) || !Enum.IsDefined(content) || batch.BatchId == Guid.Empty) throw new ArgumentException("Invalid report request.");
        var settings = options.Value;
        var uri = new Uri(settings.ServiceUrl, UriKind.Absolute);
        if ((uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)) || !string.IsNullOrEmpty(uri.UserInfo)
            || !settings.ReportPath.StartsWith("/SyncBridge/", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid SSRS deployment configuration.");

        var loaded = await InvokeAsync(uri, "LoadReport", null,
            [new(Execution + "Report", settings.ReportPath)], cancellationToken);
        // SSRS repeats ExecutionID inside ExecutionInfo as well as the SOAP header.
        var executionId = loaded.Descendants(Execution + "ExecutionHeader").Elements(Execution + "ExecutionID").SingleOrDefault()?.Value
            ?? throw new InvalidOperationException("SSRS execution header is missing.");
        var view = content switch { ReportContent.BatchSummary => "Summary", ReportContent.Deviations => "Deviations", _ => "All" };
        await InvokeAsync(uri, "SetExecutionParameters", executionId,
            [new(Execution + "Parameters",
                Parameter("BatchId", batch.BatchId.ToString("D")), Parameter("View", view)), new(Execution + "ParameterLanguage", "en-US")], cancellationToken);
        const string htmlDeviceInfo = "<DeviceInfo><HTMLFragment>False</HTMLFragment><Toolbar>False</Toolbar><JavaScript>False</JavaScript><StyleStream>False</StyleStream><Section>0</Section></DeviceInfo>";
        var result = await InvokeAsync(uri, "Render", executionId,
            [new(Execution + "Format", format == ReportFormat.Pdf ? "PDF" : "HTML5"), new(Execution + "DeviceInfo", format == ReportFormat.Pdf ? "<DeviceInfo/>" : htmlDeviceInfo)], cancellationToken);
        var bytes = Convert.FromBase64String(Value(result, "Result"));
        var mime = Value(result, "MimeType");
        if (format == ReportFormat.Pdf && (mime != "application/pdf" || !bytes.AsSpan().StartsWith("%PDF-"u8)))
            throw new InvalidOperationException("Invalid SSRS PDF response.");
        if (format == ReportFormat.Browser && !mime.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid SSRS HTML response.");
        var safeNumber = new string(batch.BatchNumber.Select(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_').ToArray());
        return new(bytes, format == ReportFormat.Pdf ? "application/pdf" : "text/html; charset=utf-8", $"{safeNumber}-{view}.{(format == ReportFormat.Pdf ? "pdf" : "html")}");
    }

    private static XElement Parameter(string name, string value) => new(Execution + "ParameterValue", new XElement(Execution + "Name", name), new XElement(Execution + "Value", value));

    private async Task<XDocument> InvokeAsync(Uri uri, string action, string? executionId, XElement[] arguments, CancellationToken cancellationToken)
    {
        var envelope = new XDocument(new XElement(Soap + "Envelope",
            new XAttribute(XNamespace.Xmlns + "s", Soap),
            new XElement(Soap + "Header", executionId is null ? null : new XElement(Execution + "ExecutionHeader", new XElement(Execution + "ExecutionID", executionId))),
            new XElement(Soap + "Body", new XElement(Execution + action, arguments))));
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("SOAPAction", Execution.NamespaceName + "/" + action);
        request.Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaximumResponseCharacters });
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        if (document.Descendants(Soap + "Fault").Any()) throw new InvalidOperationException("SSRS could not render the requested report.");
        return document;
    }

    private static string Value(XDocument document, string localName) =>
        document.Descendants(Execution + localName).SingleOrDefault()?.Value
        ?? throw new InvalidOperationException("Incomplete SSRS response.");
}
