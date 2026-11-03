using System.Data;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Infrastructure.Identity;

namespace SyncBridge.Core.Tests;

public sealed class LiveReportingFactAttribute : FactAttribute
{
    public LiveReportingFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SYNCBRIDGE_APPROVED_REPORTING") != "1")
            Skip = "Explicit opt-in required for read-only localhost SyncBridgeCore and deployed SSRS verification.";
    }
}

/// <summary>Existing operational evidence only; no seeding, mutation, migrations, real credential changes or database lifecycle.</summary>
public sealed class LiveBatchReportingTests
{
    [LiveReportingFact]
    public async Task DeployedEbr_ReconcilesEveryFieldAcrossTwoBatches_AndRendersThroughAuthorizedRazor()
    {
        var root = FindRoot(); var evidence = Environment.GetEnvironmentVariable("SYNCBRIDGE_REPORT_EVIDENCE")
            ?? Path.Combine(root, "artifacts/test-evidence/sprint-7/batch-a4");
        Directory.CreateDirectory(evidence);
        const string connectionString = "Server=localhost;Database=SyncBridgeCore;Integrated Security=True;TrustServerCertificate=True";
        await using var connection = new SqlConnection(connectionString); await connection.OpenAsync();
        Assert.Equal("SyncBridgeCore", connection.Database);
        var batches = new List<(Guid Id, string Number)>();
        await using (var cmd = new SqlCommand("SELECT TOP (2) BatchId,BatchNumber FROM dbo.Batch WHERE Status=N'Completed' ORDER BY CreatedAt,BatchId", connection))
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync()) batches.Add((reader.GetGuid(0), reader.GetString(1)));
        Assert.Equal(2, batches.Count);

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:SyncBridgeCore", connectionString);
            builder.ConfigureServices(services => services.PostConfigure<ManufacturingIdentityOptions>(options =>
            {
                // Existing active actors, test-host-only credentials. Actual User Secrets are neither read nor changed here.
                options.Accounts = [Account("demo.operator", "Production Operator"), Account("demo.quality", "Quality Engineer")];
            }));
        });
        using var reviewer = await BatchReportingTests.Login(factory, "demo.quality");
        using var productionOperator = await BatchReportingTests.Login(factory, "demo.operator");
        Assert.Equal(HttpStatusCode.Forbidden, (await productionOperator.GetAsync("/Batches/Report?number=" + batches[0].Number)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await reviewer.GetAsync("/Batches/Report?number=NOT-A-PERSISTED-BATCH")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await reviewer.GetAsync("/Batches/Report?number=" + batches[0].Number + "&format=999")).StatusCode);
        var rdl = XDocument.Load(Path.Combine(root, "src/SyncBridge.Core.Infrastructure/Reporting/Reports/ElectronicBatchRecord.rdl"));
        var ns = rdl.Root!.Name.Namespace;
        var timings = new List<object>(); var reconciliation = new List<object>();
        using var native = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true, AllowAutoRedirect = false, UseProxy = false });
        foreach (var batch in batches)
        {
            var watch = Stopwatch.StartNew();
            var html = await reviewer.GetAsync("/Batches/Report?number=" + Uri.EscapeDataString(batch.Number) + "&batchId=" + Guid.NewGuid());
            var htmlBytes = await html.Content.ReadAsByteArrayAsync(); watch.Stop();
            Assert.Equal(HttpStatusCode.OK, html.StatusCode); Assert.True(html.Headers.CacheControl!.NoStore);
            Assert.Equal("text/html", html.Content.Headers.ContentType!.MediaType);
            var htmlText = Encoding.UTF8.GetString(htmlBytes);
            Assert.Contains(batch.Number, htmlText); Assert.Contains("SB-DEV-01\\SyncBridgeReport", htmlText);
            await File.WriteAllBytesAsync(Path.Combine(evidence, batch.Number + ".html"), htmlBytes);
            timings.Add(new { Batch = batch.Number, Format = "Browser", Milliseconds = watch.Elapsed.TotalMilliseconds });
            watch.Restart();
            var pdf = await reviewer.GetAsync("/Batches/Report?number=" + Uri.EscapeDataString(batch.Number) + "&format=Pdf");
            var pdfBytes = await pdf.Content.ReadAsByteArrayAsync(); watch.Stop();
            Assert.Equal(HttpStatusCode.OK, pdf.StatusCode); Assert.True(pdfBytes.AsSpan().StartsWith("%PDF-"u8));
            Assert.True(pdfBytes.Length > 1000);
            await File.WriteAllBytesAsync(Path.Combine(evidence, batch.Number + ".pdf"), pdfBytes);
            timings.Add(new { Batch = batch.Number, Format = "Pdf", Milliseconds = watch.Elapsed.TotalMilliseconds });
            var xmlBytes = await native.GetByteArrayAsync("http://127.0.0.1/ReportServer?/SyncBridge/ElectronicBatchRecord&rs:Format=XML&BatchId=" + batch.Id + "&View=All");
            await File.WriteAllBytesAsync(Path.Combine(evidence, batch.Number + ".xml"), xmlBytes);
            using var xmlStream = new MemoryStream(xmlBytes);
            var rendered = XDocument.Load(xmlStream);
            var values = rendered.Descendants().Attributes().Select(a => a.Value).Concat(rendered.Descendants().Where(e => !e.HasElements).Select(e => e.Value)).ToArray();
            Assert.Contains("SB-DEV-01\\SyncBridgeReport", string.Join("\n", values));
            Assert.DoesNotContain(batches.Single(b => b.Id != batch.Id).Id.ToString("D"), string.Join("\n", values), StringComparison.OrdinalIgnoreCase);
            await using (var impersonate = new SqlCommand("EXECUTE AS LOGIN=N'SB-DEV-01\\SyncBridgeReport';", connection)) await impersonate.ExecuteNonQueryAsync();
            try
            {
                foreach (var dataset in rdl.Descendants(ns + "DataSet"))
                {
                    var name = dataset.Attribute("Name")!.Value;
                    var query = dataset.Descendants(ns + "CommandText").Single().Value;
                    Assert.Contains("b.BatchId=TRY_CONVERT(uniqueidentifier,@BatchId)", query);
                    Assert.Contains("b.Status=N'Completed'", query);
                    Assert.Contains("ORDER BY", query);
                    await using var cmd = new SqlCommand(query, connection); cmd.Parameters.Add("@BatchId", SqlDbType.NVarChar, 100).Value = batch.Id.ToString("D");
                    var records = new List<string>();
                    await using (var reader = await cmd.ExecuteReaderAsync())
                        while (await reader.ReadAsync())
                        {
                            var record = string.Join("\n", Enumerable.Range(0, reader.FieldCount).Select(i => Regex.Replace(reader.GetName(i), "(?<!^)(?=[A-Z])", " ") + ": " + reader.GetString(i)));
                            Assert.Contains(values, value => Normalize(value) == Normalize(record));
                            records.Add(record);
                        }
                    Assert.NotEmpty(records);
                    // One native XML record per query row, preserving order, identifiers, actors and every field.
                    var renderedRecords = rendered.Descendants().Attributes().Where(a => a.Name.LocalName == name + "Record").Select(a => a.Value)
                        .Concat(rendered.Descendants().Where(e => e.Name.LocalName == name + "Record").Select(e => e.Value)).ToArray();
                    Assert.Equal(records.Select(Normalize), renderedRecords.Select(Normalize));
                    cmd.Parameters["@BatchId"].Value = batch.Id + "' OR 1=1--";
                    await using var invalidReader = await cmd.ExecuteReaderAsync(); Assert.False(await invalidReader.ReadAsync());
                    reconciliation.Add(new { Batch = batch.Number, Dataset = name, Rows = records.Count, EveryFieldAndOrderMatched = true, ParameterTamperingReturnsNoRows = true, Records = records });
                }
            }
            finally { await using var revert = new SqlCommand("REVERT", connection); await revert.ExecuteNonQueryAsync(); }
        }
        foreach (var mode in new[] { "BatchSummary", "Deviations" })
        {
            var response = await reviewer.GetAsync("/Batches/Report?number=" + batches[0].Number + "&content=" + mode);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            await File.WriteAllTextAsync(Path.Combine(evidence, mode + ".html"), await response.Content.ReadAsStringAsync());
        }
        await File.WriteAllTextAsync(Path.Combine(evidence, "Timings.json"), JsonSerializer.Serialize(timings, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(evidence, "Dataset_Reconciliation.json"), JsonSerializer.Serialize(reconciliation, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();
    private static ManufacturingAccount Account(string user, string role) => new()
    { UserId = user, UserName = user, PasswordHash = new PasswordHasher<ManufacturingAccount>().HashPassword(new(), SecuredServiceFixture.Password), Roles = [role] };
    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "src/SyncBridge.Core.slnx"))) return dir.FullName;
        throw new InvalidOperationException("Repository root not found.");
    }
}
