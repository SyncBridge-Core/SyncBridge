using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;
using Xunit.Abstractions;

namespace SyncBridge.Core.Tests;

public sealed class UiApiEquivalenceTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task PairedHttpWorkflows_HaveEquivalentManufacturingEvidence(string environment)
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await EquivalenceScenario.Run(fixture, SecuredServiceFixture.Supervisor, SecuredServiceFixture.Quality, environment, output);
    }
}

[Collection(SqlServerRecoveryCollection.Name)]
[Trait("Category", "LiveSqlServer")]
public sealed class RestSqlEquivalenceTests(ITestOutputHelper output)
{
    [ApprovedSqlServerFact]
    public async Task PairedHttpWorkflows_PersistEquivalentEvidenceThroughActualSqlServer()
    {
        await using var fixture = SecuredServiceFixture.CreateSqlServer();
        var orders = new[] { "S4B-" + Guid.NewGuid().ToString("N"), "S4B-" + Guid.NewGuid().ToString("N") };
        try
        {
            await EquivalenceScenario.Run(fixture, SecuredServiceFixture.Operator, "demo.quality", "Production", output, orders, sqlServer: true);
        }
        finally
        {
            foreach (var order in orders) await SqlServerBatchBIntegrationTests.Cleanup(fixture, order);
        }
        await using var scope = fixture.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.Equal(2, await db.ProductionOrders.CountAsync());
        Assert.Equal(0, await db.Batches.CountAsync());
        Assert.Equal(0, await db.AuditEvents.CountAsync());
        Assert.Equal(0, await db.BatchStepExecutions.CountAsync());
        Assert.Equal(0, await db.Deviations.CountAsync());
        Assert.Equal(0, await db.ElectronicSignatures.CountAsync());
        output.WriteLine("SQL HTTP cleanup: two seeded orders, no batch/step/deviation/signature/audit history remains.");
    }
}

// One executable comparison drives real Pages and REST; no manufacturing service mocks.
internal static class EquivalenceScenario
{
    internal static async Task Run(SecuredServiceFixture f, string supervisor, string quality, string environment,
        ITestOutputHelper output, string[]? numbers = null, bool sqlServer = false)
    {
        numbers ??= ["EQ-UI", "EQ-API"];
        await using var host = HttpManufacturing.Factory(f, environment);
        using var client = await HttpManufacturing.Login(host, SecuredServiceFixture.Operator);
        using var reviewer = await HttpManufacturing.Login(host, quality);
        Assert.Equal(environment, host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            Assert.Equal(sqlServer, db.Database.IsSqlServer());
            if (sqlServer) Assert.Equal(SqlServerTestDatabase.DatabaseName, db.Database.GetDbConnection().Database);
        }
        var summaries = new List<string>();
        for (var index = 0; index < 2; index++)
        {
            var api = index == 1;
            var order = numbers[index]; var batch = order + "B"; var deviation = order + "D";
            var createdOrder = await f.Order(supervisor, s => s.CreateAsync(new(order, "EQ-P", "Fictional equivalence product", "RCP-DEMO-001", 12.125m)));
            Assert.True(createdOrder.Succeeded);
            var initial = await HistoryCounts(f);
            var orderPage = await client.GetStringAsync("/Orders/Details?number=" + order);
            var orderJson = (await client.GetFromJsonAsync<ProductionOrderView>("/api/production-orders/" + order))!;
            Assert.Equal(createdOrder.Value, orderJson);
            foreach (var value in new[] { orderJson.OrderNumber, orderJson.ProductCode, orderJson.ProductName, orderJson.RecipeId, "12.125", orderJson.Status.ToString() })
                Assert.Contains(value, orderPage);
            Assert.Equal(initial, await HistoryCounts(f));
            await Command("Initiate", new { orderNumber = order, batchNumber = batch }, 422);
            Assert.Equal(initial, await HistoryCounts(f));
            Assert.True((await f.Order(supervisor, s => s.ChangeStatusAsync(order, ProductionOrderAction.Release))).Succeeded);
            await Command("Initiate", new { orderNumber = order, batchNumber = batch, actorId = quality }, api ? 201 : 302);
            Assert.Equal(BatchStatus.Created, (await Read()).Status);
            var createdAudit = await Audits();
            Assert.Single(createdAudit);
            Assert.All(createdAudit, a => Assert.Equal(SecuredServiceFixture.Operator, a.ActorId));
            await Reject("Initiate", new { orderNumber = order, batchNumber = batch });
            await Reject("StartStep", new { stepNumber = 1 });
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.MarkReadyAsync(batch))).Succeeded);
            await StartRejected(); // both mandatory prerequisites absent
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Passed))).Succeeded);
            await StartRejected(); // material alone cannot replace equipment
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync(batch, "EQ-DEMO-001", VerificationResult.Failed))).Succeeded);
            await StartRejected();
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync(batch, "EQ-DEMO-001", VerificationResult.Passed))).Succeeded);
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Failed))).Succeeded);
            await StartRejected(); // equipment alone cannot replace material
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync(batch, "MAT-DEMO-001", VerificationResult.Passed))).Succeeded);
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync(batch))).Succeeded);
            await Reject("StartStep", new { stepNumber = 2 });
            await Command("StartStep", new { stepNumber = 1 }, api ? 200 : 302);
            var first = (await Read()).Steps.Single();
            var rejected = await Snapshot();
            var foreignExecution = index == 0 ? Guid.NewGuid().ToString("D") :
                (await f.Batch(supervisor, s => s.GetAsync(numbers[0] + "B"))).Value!.Steps.First().ExecutionId;
            await Command("Deviation", new { deviationCode = deviation, description = "Foreign activity", executionId = foreignExecution }, 404);
            Assert.Equal(rejected, await Snapshot());
            f.FailAfterFirstWrite();
            var failed = await Command("Deviation", new { deviationCode = deviation, description = "Rolled back", executionId = first.ExecutionId }, 503);
            Assert.DoesNotContain("private diagnostic", failed);
            Assert.Equal(rejected, await Snapshot());
            await Command("Deviation", new { deviationCode = deviation, description = "Equivalent interruption", executionId = first.ExecutionId, actorId = quality }, api ? 200 : 302);
            var exceptional = await Read();
            Assert.Equal(BatchStatus.Exception, exceptional.Status);
            Assert.Equal(first.ExecutionId, exceptional.Deviations.Single().ExecutionId);
            Assert.Equal(DeviationStatus.Open, exceptional.Deviations.Single().Status);
            await Reject("CompleteStep", new { executionId = first.ExecutionId });
            Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.ResumeAsync(batch))).Outcome);
            Assert.True((await f.Batch(quality, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.BeginReview, "Reviewed"))).Succeeded);
            Assert.True((await f.Batch(quality, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.Approve))).Succeeded);
            Assert.True((await f.Batch(quality, s => s.ReviewDeviationAsync(batch, deviation, DeviationAction.Resolve, "Resolved"))).Succeeded);
            Assert.Equal(BatchStatus.Exception, (await Read()).Status);
            Assert.True((await f.Batch(SecuredServiceFixture.Operator, s => s.ResumeAsync(batch))).Succeeded);
            await Command("CompleteStep", new { executionId = first.ExecutionId, recordedData = "Equivalent evidence" }, api ? 200 : 302);
            await Command("StartStep", new { stepNumber = 2 }, api ? 200 : 302);
            var second = (await Read()).Steps.Single(s => s.StepNumber == 2);
            await Reject("CompleteStep", new { executionId = second.ExecutionId });
            // Required approval remains on the accepted Page, for both paths.
            await HttpManufacturing.Form(reviewer, $"/Batches/Details?number={batch}&handler=Sign",
                new { executionId = second.ExecutionId, meaning = "Approval" }, 302);
            var signature = (await Read()).Signatures.Single();
            Assert.Equal(quality, signature.ActorId);
            Assert.Equal(second.ExecutionId, signature.ExecutionId);
            await Command("CompleteStep", new { executionId = second.ExecutionId, signatureId = signature.SignatureId, recordedData = "Signed evidence" }, api ? 200 : 302);
            Assert.Single((await Read()).Signatures); // completion references, never creates another signature
            Assert.True((await f.Batch(supervisor, s => s.CompleteAsync(batch))).Succeeded);
            await Reject("StartStep", new { stepNumber = 1 });
            await Reject("CompleteStep", new { executionId = second.ExecutionId, signatureId = signature.SignatureId });
            await Reject("Deviation", new { deviationCode = deviation + "X", description = "Late change" });
            var final = await Read();
            var audits = await Audits();
            Assert.Equal(BatchStatus.Completed, final.Status);
            Assert.Equal(audits.Count, audits.Select(a => a.Id).Distinct().Count());
            Assert.Equal(AuditEventType.BatchCreated, audits.First().Action);
            Assert.Equal(AuditEventType.BatchCompleted, audits.Last().Action);
            await using (var scope = f.Scope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
                var row = await db.Batches.SingleAsync(b => b.BatchNumber == batch);
                Assert.NotNull(row.StartedAt); Assert.NotNull(row.CompletedAt);
                Assert.True(row.CreatedAt <= row.StartedAt && row.StartedAt <= row.CompletedAt);
                var steps = await db.BatchStepExecutions.Where(s => s.BatchId == row.BatchId).OrderBy(s => s.StepNumber).ToArrayAsync();
                Assert.Equal(2, steps.Length);
                Assert.All(steps, s =>
                {
                    Assert.Equal(StepExecutionStatus.Completed, s.Status);
                    Assert.NotNull(s.StartedAt); Assert.NotNull(s.CompletedAt);
                    Assert.True(s.CompletedAt >= s.StartedAt);
                    Assert.True(s.StartedAt >= row.StartedAt && s.CompletedAt <= row.CompletedAt);
                    Assert.Equal(SecuredServiceFixture.Operator, s.CompletedByUserId);
                });
                Assert.Equal("Equivalent evidence", steps[0].RecordedData);
                Assert.Equal("Signed evidence", steps[1].RecordedData);
                var dev = await db.Deviations.SingleAsync(d => d.BatchId == row.BatchId);
                Assert.Equal(steps[0].StepExecutionId, dev.StepExecutionId);
                Assert.Equal(SecuredServiceFixture.Operator, dev.ReportedByUserId);
                Assert.Equal(DeviationStatus.Closed, dev.Status);
                var sig = await db.ElectronicSignatures.SingleAsync(s => s.BatchId == row.BatchId);
                Assert.Equal("BatchStepExecution", sig.RelatedRecordType);
                Assert.Equal(steps[1].StepExecutionId, sig.RelatedRecordId);
                Assert.Equal(quality, sig.UserId);
                Assert.True(sig.SignedAt <= steps[1].CompletedAt);
                Assert.Equal(audits.Count, await db.AuditEvents.CountAsync(a => a.BatchId == row.BatchId));
            }
            // Normalize generated identifiers/times and batch-specific references, retain business evidence.
            var normalized = JsonSerializer.Serialize(new
            {
                final.Status, final.PlannedQuantity, final.RecipeCode,
                Steps = final.Steps.OrderBy(s => s.StepNumber).Select(s => new { s.StepNumber, s.Status, s.RequiresSignature, s.RecordedData }),
                Deviations = final.Deviations.Select(d => new { d.Description, d.Status, d.ReviewComments, d.FinalDisposition }),
                Signatures = final.Signatures.Select(s => new { s.ActorId, s.Meaning }),
                Audit = audits.OrderBy(a => a.OccurredAt).ThenBy(a => a.Action).Select(a => new { a.Action, a.ActorId, a.ResultingState })
            });
            summaries.Add(normalized);
            output.WriteLine($"{(api ? "REST" : "Razor")} {environment}: {normalized}");

            async Task<BatchView> Read() => (await f.Batch(supervisor, s => s.GetAsync(batch))).Value!;
            async Task<IReadOnlyList<AuditView>> Audits() => (await f.Batch(supervisor, s => s.GetAuditAsync(batch))).Value!;
            async Task<string> Snapshot() => JsonSerializer.Serialize(new { Batch = await Read(), Audit = await Audits() });
            async Task Reject(string command, object body)
            {
                var before = await Snapshot();
                await Command(command, body, 422);
                Assert.Equal(before, await Snapshot());
            }
            async Task StartRejected()
            {
                var before = await Snapshot();
                Assert.Equal(OperationOutcome.BusinessRuleRejected, (await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync(batch))).Outcome);
                await Command("StartStep", new { stepNumber = 1 }, 422);
                Assert.Equal(before, await Snapshot());
            }
            Task<string> Command(string command, object body, int status)
            {
                var values = JsonSerializer.SerializeToElement(body);
                var route = command switch
                {
                    "Initiate" => "/api/batches",
                    "StartStep" => $"/api/batches/{batch}/steps/{values.GetProperty("stepNumber").GetInt32()}/start",
                    "CompleteStep" => $"/api/batches/{batch}/steps/{values.GetProperty("executionId").GetString()}/complete",
                    _ => $"/api/batches/{batch}/deviations"
                };
                return api ? HttpManufacturing.Json(client, route, body, status)
                    : HttpManufacturing.Form(client, command == "Initiate" ? "/Batches/Initiate" : $"/Batches/Details?number={batch}&handler={command}", body, status);
            }
        }
        Assert.Equal(summaries[0], summaries[1]);
        output.WriteLine("PASS: normalized UI/API state, ordered execution/data, deviation, signature and exact audit action/actor/state sequence match.");
    }

    private static async Task<string> HistoryCounts(SecuredServiceFixture fixture)
    {
        await using var scope = fixture.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        return $"{await db.Batches.CountAsync()}:{await db.AuditEvents.CountAsync()}:{await db.BatchStepExecutions.CountAsync()}:{await db.Deviations.CountAsync()}:{await db.ElectronicSignatures.CountAsync()}";
    }
}

internal static class HttpManufacturing
{
    internal static WebApplicationFactory<Program> Factory(SecuredServiceFixture f, string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("environment", environment);
            b.UseSetting("ConnectionStrings:SyncBridgeCore", "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True");
            b.ConfigureServices(f.ConfigureTestHost);
        });
    internal static HttpClient Client(WebApplicationFactory<Program> host) => host.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    internal static async Task<string> Token(HttpClient client)
    {
        var html = await client.GetStringAsync("/authentication");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    internal static async Task<HttpClient> Login(WebApplicationFactory<Program> host, string actor)
    {
        var client = Client(host);
        await Form(client, "/authentication/login", new { userName = actor, password = SecuredServiceFixture.Password }, 302);
        return client;
    }
    internal static async Task<string> Form(HttpClient client, string route, object body, int status)
    {
        var form = JsonSerializer.SerializeToElement(body).EnumerateObject()
            .Where(p => p.Value.ValueKind != JsonValueKind.Null).ToDictionary(p => p.Name, p => p.Value.ToString());
        form["__RequestVerificationToken"] = await Token(client);
        using var response = await client.PostAsync(route, new FormUrlEncodedContent(form));
        Assert.Equal(status, (int)response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }
    internal static async Task<string> Json(HttpClient client, string route, object body, int status, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", token ?? await Token(client));
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Contains("json", response.Content.Headers.ContentType!.MediaType!);
        return await response.Content.ReadAsStringAsync();
    }
}
