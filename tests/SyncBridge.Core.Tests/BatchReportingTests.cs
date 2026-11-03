using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyncBridge.Core.Application.Contracts.Reporting;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;
using SyncBridge.Core.Infrastructure.Reporting;

namespace SyncBridge.Core.Tests;

public sealed class BatchReportingTests
{
    [Theory]
    [InlineData(null, OperationOutcome.Unauthenticated)]
    [InlineData(SecuredServiceFixture.Operator, OperationOutcome.Forbidden)]
    [InlineData(SecuredServiceFixture.Administrator, OperationOutcome.Forbidden)]
    public async Task UnauthenticatedAndNonReviewRolesCannotProbeBatchExistence(string? actor, OperationOutcome expected)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        var renderer = new RecordingRenderer();
        var result = await Generate(f, actor, renderer, "does-not-exist");
        Assert.Equal(expected, result.Outcome);
        Assert.Empty(renderer.Calls);
    }

    [Fact]
    public async Task IncompleteAndMissingBatchesAreRejectedBeforeRendering()
    {
        await using var f = await SecuredServiceFixture.CreateAsync(); await f.PrepareAsync();
        var renderer = new RecordingRenderer();
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await Generate(f, SecuredServiceFixture.Supervisor, renderer, "B-TEST")).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await Generate(f, SecuredServiceFixture.Quality, renderer, "B-MISSING")).Outcome);
        Assert.Empty(renderer.Calls);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 99)]
    public async Task InvalidOptionsAreRejected(int format, int content)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        var renderer = new RecordingRenderer();
        Assert.Equal(OperationOutcome.InvalidInput, (await Generate(f, SecuredServiceFixture.Quality, renderer, "B", (ReportFormat)format, (ReportContent)content)).Outcome);
        Assert.Empty(renderer.Calls);
    }

    [Fact]
    public async Task CompletedReportsResolveExactPersistedBatchAndPreserveAllEvidence()
    {
        await using var f = await SecuredServiceFixture.CreateAsync(); await Complete(f);
        Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.InitiateAsync("PO-TEST", "B-OTHER"))).Succeeded);
        var renderer = new RecordingRenderer();
        var before = await Snapshot(f);
        Assert.True((await Generate(f, SecuredServiceFixture.Quality, renderer, "B-TEST", ReportFormat.Pdf)).Succeeded);
        Assert.Equal(OperationOutcome.BusinessRuleRejected, (await Generate(f, SecuredServiceFixture.Quality, renderer, "B-OTHER")).Outcome);
        Assert.Equal(OperationOutcome.NotFound, (await Generate(f, SecuredServiceFixture.Quality, renderer, "B-TEST' OR 1=1--")).Outcome);
        Assert.Single(renderer.Calls);
        await using var scope = f.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.Equal((await db.Batches.SingleAsync(b => b.BatchNumber == "B-TEST")).BatchId, renderer.Calls[0].BatchId);
        Assert.Equal(before, await Snapshot(f));
    }

    [Fact]
    public async Task ServiceFailureIsControlledAndDoesNotDiscloseDiagnostics()
    {
        await using var f = await SecuredServiceFixture.CreateAsync(); await Complete(f);
        var renderer = new RecordingRenderer { Fail = true };
        var result = await Generate(f, SecuredServiceFixture.Supervisor, renderer, "B-TEST");
        Assert.Equal(OperationOutcome.TechnicalFailure, result.Outcome);
        Assert.DoesNotContain("private", result.Message!);
        Assert.Equal(BatchStatus.Completed, (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAsync("B-TEST"))).Value!.Status);
    }

    [Fact]
    public async Task RazorReportRequiresCookieAndReviewRoleAndRejectsBindingTampering()
    {
        await using var f = await SecuredServiceFixture.CreateAsync(); await Complete(f);
        var renderer = new RecordingRenderer();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:SyncBridgeCore", "Server=unused;Database=SyncBridgeCore;Integrated Security=True");
            builder.ConfigureServices(services => { f.ConfigureTestHost(services); services.AddSingleton<IBatchReportRenderer>(renderer); });
        });
        using var anonymous = Client(factory);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/Batches/Report?number=B-TEST")).StatusCode);
        using var productionOperator = await Login(factory, SecuredServiceFixture.Operator);
        Assert.Equal(HttpStatusCode.Forbidden, (await productionOperator.GetAsync("/Batches/Report?number=B-TEST")).StatusCode);
        Assert.DoesNotContain("View Electronic Batch Record", await productionOperator.GetStringAsync("/Batches/Details?number=B-TEST"));
        using var quality = await Login(factory, SecuredServiceFixture.Quality);
        Assert.Contains("View Electronic Batch Record", await quality.GetStringAsync("/Batches/Details?number=B-TEST"));
        var html = await quality.GetAsync("/Batches/Report?number=B-TEST&batchId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, html.StatusCode); Assert.True(html.Headers.CacheControl!.NoStore);
        Assert.Contains("sandbox", html.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("text/html", html.Content.Headers.ContentType!.MediaType);
        Assert.Single(renderer.Calls); Assert.Equal("B-TEST", renderer.Calls[0].BatchNumber);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await quality.GetAsync("/Batches/Report?number=B-TEST&format=exe")).StatusCode);
        var pdf = await quality.GetAsync("/Batches/Report?number=B-TEST&format=Pdf&content=Deviations");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.NotNull(pdf.Content.Headers.ContentDisposition);
        renderer.Fail = true;
        var failed = await quality.GetAsync("/Batches/Report?number=B-TEST");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.DoesNotContain("private", await failed.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(ReportContent.ElectronicBatchRecord)]
    [InlineData(ReportContent.BatchSummary)]
    [InlineData(ReportContent.Deviations)]
    public async Task AuthorizedViewModesUseSameCompletedBatch(ReportContent content)
    {
        await using var f = await SecuredServiceFixture.CreateAsync(); await Complete(f);
        var renderer = new RecordingRenderer();
        Assert.True((await Generate(f, SecuredServiceFixture.Supervisor, renderer, "B-TEST", content: content)).Succeeded);
        Assert.Single(renderer.Calls);
    }

    internal static async Task Complete(SecuredServiceFixture f)
    {
        await f.StartAsync();
        foreach (var step in new[] { 1, 2 })
        {
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.StartStepAsync("B-TEST", step))).Succeeded);
            var execution = (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAsync("B-TEST"))).Value!.Steps.Single(s => s.StepNumber == step);
            string? signature = null;
            if (execution.RequiresSignature)
            {
                var signed = await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", execution.ExecutionId, ElectronicSignatureMeaning.Approval));
                Assert.True(signed.Succeeded); signature = signed.Value!.Signatures.Single().SignatureId;
            }
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.CompleteStepAsync("B-TEST", execution.ExecutionId, signature, "Actual test workflow evidence"))).Succeeded);
        }
        Assert.True((await f.Batch(SecuredServiceFixture.Supervisor, s => s.CompleteAsync("B-TEST"))).Succeeded);
    }

    private static async Task<OperationResult<ReportDocument>> Generate(SecuredServiceFixture f, string? actor, RecordingRenderer renderer, string number,
        ReportFormat format = ReportFormat.Browser, ReportContent content = ReportContent.ElectronicBatchRecord)
    {
        await using var scope = f.Scope(actor);
        return await new BatchReportService(scope.ServiceProvider.GetRequiredService<ApplicationOperation>(),
            scope.ServiceProvider.GetRequiredService<IReportBatchReader>(), renderer).GenerateAsync(number, format, content);
    }
    private static async Task<string> Snapshot(SecuredServiceFixture f)
    {
        await using var scope = f.Scope(); var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        return JsonSerializer.Serialize(new { Batches = await db.Batches.AsNoTracking().OrderBy(b => b.BatchNumber).ToArrayAsync(),
            Steps = await db.BatchStepExecutions.AsNoTracking().OrderBy(s => s.StepNumber).ToArrayAsync(),
            Materials = await db.MaterialVerifications.AsNoTracking().ToArrayAsync(), Equipment = await db.EquipmentVerifications.AsNoTracking().ToArrayAsync(),
            Deviations = await db.Deviations.AsNoTracking().ToArrayAsync(), Signatures = await db.ElectronicSignatures.AsNoTracking().ToArrayAsync(), Audit = await db.AuditEvents.AsNoTracking().ToArrayAsync() });
    }
    internal static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    internal static async Task<HttpClient> Login(WebApplicationFactory<Program> factory, string actor)
    {
        var client = Client(factory); var html = await client.GetStringAsync("/authentication");
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var response = await client.PostAsync("/authentication/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["userName"] = actor, ["password"] = SecuredServiceFixture.Password, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); return client;
    }
    private sealed class RecordingRenderer : IBatchReportRenderer
    {
        public List<ReportBatch> Calls { get; } = []; public bool Fail { get; set; }
        public Task<ReportDocument> RenderAsync(ReportBatch batch, ReportFormat format, ReportContent content, CancellationToken cancellationToken)
        {
            Calls.Add(batch); if (Fail) throw new HttpRequestException("private SSRS diagnostics");
            return Task.FromResult(new ReportDocument(Encoding.UTF8.GetBytes(batch.BatchNumber), format == ReportFormat.Pdf ? "application/pdf" : "text/html; charset=utf-8", "report.pdf"));
        }
    }
}

public sealed class SsrsWireTests
{
    [Fact]
    public async Task NativeSoapUsesServerExecutionContextAndExactResolvedBatch()
    {
        var handler = new SoapHandler(); var renderer = new SsrsReportRenderer(new HttpClient(handler), Options.Create(new SsrsReportingOptions()));
        var id = Guid.NewGuid(); var document = await renderer.RenderAsync(new(id, "B/unsafe", BatchStatus.Completed), ReportFormat.Pdf, ReportContent.Deviations, default);
        Assert.Equal("application/pdf", document.ContentType); Assert.Equal("B_unsafe-Deviations.pdf", document.FileName);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains(id.ToString("D"), handler.Requests[1]); Assert.Contains("Deviations", handler.Requests[1]);
        Assert.Contains("server-execution", handler.Requests[2]); Assert.DoesNotContain("B/unsafe", handler.Requests[1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UnavailableOrMalformedServerDoesNotProduceDocument(bool httpFailure)
    {
        var handler = new SoapHandler { HttpFailure = httpFailure, BadPdf = !httpFailure };
        var renderer = new SsrsReportRenderer(new HttpClient(handler), Options.Create(new SsrsReportingOptions()));
        await Assert.ThrowsAnyAsync<Exception>(() => renderer.RenderAsync(new(Guid.NewGuid(), "B", BatchStatus.Completed), ReportFormat.Pdf, ReportContent.ElectronicBatchRecord, default));
    }

    [Fact]
    public async Task RemotePlaintextCredentialTargetIsRejectedBeforeNetworkAccess()
    {
        var handler = new SoapHandler(); var renderer = new SsrsReportRenderer(new HttpClient(handler), Options.Create(new SsrsReportingOptions { ServiceUrl = "http://untrusted.example/report" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync(new(Guid.NewGuid(), "B", BatchStatus.Completed), ReportFormat.Pdf, ReportContent.ElectronicBatchRecord, default));
        Assert.Empty(handler.Requests);
    }
    private sealed class SoapHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = []; public bool HttpFailure { get; set; } public bool BadPdf { get; set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (HttpFailure) return new(HttpStatusCode.ServiceUnavailable);
            var ns = XNamespace.Get("http://schemas.microsoft.com/sqlserver/2005/06/30/reporting/reportingservices");
            var value = Requests.Count switch { 1 => new XElement("Header", new XElement(ns + "ExecutionHeader", new XElement(ns + "ExecutionID", "server-execution")), new XElement(ns + "ExecutionInfo", new XElement(ns + "ExecutionID", "server-execution"))), 2 => new XElement(ns + "SetExecutionParametersResponse"), _ => new XElement(ns + "RenderResponse", new XElement(ns + "Result", Convert.ToBase64String(Encoding.UTF8.GetBytes(BadPdf ? "bad" : "%PDF-test"))), new XElement(ns + "MimeType", "application/pdf")) };
            return new(HttpStatusCode.OK) { Content = new StringContent(new XElement("Envelope", value).ToString()) };
        }
    }
}
