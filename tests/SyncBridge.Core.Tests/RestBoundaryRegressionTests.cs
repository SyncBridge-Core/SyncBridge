using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Tests;

public sealed class RestBoundaryRegressionTests
{
    [Fact]
    public async Task OptionalDeviationActivity_RemainsNullThroughBothAdapters()
    {
        foreach (var api in new[] { false, true })
        {
            await using var f = await SecuredServiceFixture.CreateAsync();
            await f.StartAsync();
            await using var host = HttpManufacturing.Factory(f);
            using var client = await HttpManufacturing.Login(host, SecuredServiceFixture.MultiRole);
            var body = new { deviationCode = "D-OPTIONAL", description = "Batch-level exception" };
            if (api) await HttpManufacturing.Json(client, "/api/batches/B-TEST/deviations", body, 200);
            else await HttpManufacturing.Form(client, "/Batches/Details?number=B-TEST&handler=Deviation", body, 302);
            var batch = (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAsync("B-TEST"))).Value!;
            Assert.Equal(SyncBridge.Core.Domain.Enumerations.BatchStatus.Exception, batch.Status);
            Assert.Null(batch.Deviations.Single().ExecutionId);
            await using var scope = f.Scope();
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            var deviation = await db.Deviations.SingleAsync();
            Assert.Null(deviation.StepExecutionId);
            Assert.Equal(SecuredServiceFixture.MultiRole, deviation.ReportedByUserId);
            Assert.Equal((await db.Batches.SingleAsync()).BatchId, deviation.BatchId);
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task ContextReads_RespectBothAdaptersWithoutMutation(string environment)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var host = HttpManufacturing.Factory(f, environment);
        using var anonymous = HttpManufacturing.Client(host);
        var challenge = await anonymous.GetAsync("/api/production-orders/PO-DEMO-001");
        Assert.Equal(HttpStatusCode.Unauthorized, challenge.StatusCode);
        Assert.Null(challenge.Headers.Location);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/Orders/Details?number=PO-DEMO-001")).StatusCode);
        using var actor = await HttpManufacturing.Login(host, SecuredServiceFixture.MultiRole);
        var context = await actor.GetFromJsonAsync<ProductionOrderView>("/api/production-orders/PO-DEMO-001");
        var page = await actor.GetStringAsync("/Orders/Details?number=PO-DEMO-001");
        Assert.Contains(context!.OrderNumber, page);
        Assert.Contains(context.ProductCode, page);
        Assert.Contains(context.ProductName, page);
        Assert.Contains(context.RecipeId, page);
        foreach (var (number, apiStatus, uiStatus) in new[] { ("MISSING", 404, 404), (new string('x', 41), 400, 422) })
        {
            Assert.Equal(apiStatus, (int)(await actor.GetAsync("/api/production-orders/" + number)).StatusCode);
            Assert.Equal(uiStatus, (int)(await actor.GetAsync("/Orders/Details?number=" + number)).StatusCode);
        }
        using var admin = await HttpManufacturing.Login(host, SecuredServiceFixture.Administrator);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/production-orders/PO-DEMO-001")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/Orders/Details?number=PO-DEMO-001")).StatusCode);
        await using var scope = f.Scope();
        var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
        Assert.Equal(0, await db.Batches.CountAsync());
        Assert.Equal(0, await db.AuditEvents.CountAsync());
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Supervisor)]
    [InlineData(SecuredServiceFixture.Quality)]
    [InlineData(SecuredServiceFixture.Administrator)]
    public async Task BothAdapters_RejectForgedAuthorityOnEverySelectedMutation(string user)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        await using var host = HttpManufacturing.Factory(f);
        using var client = await HttpManufacturing.Login(host, user);
        client.DefaultRequestHeaders.Add("X-UserId", SecuredServiceFixture.Operator);
        client.DefaultRequestHeaders.Add("X-Role", "Operator");
        var before = await Snapshot(f);
        var forged = new { orderNumber = "PO-TEST", batchNumber = "B-FORGE", deviationCode = "D-FORGE", description = "Forged", stepNumber = 1,
            executionId = Guid.NewGuid().ToString("D"), actorId = SecuredServiceFixture.Operator, role = "Operator", approvalIdentity = SecuredServiceFixture.Quality };
        foreach (var (api, ui) in new[]
        {
            ("/api/batches", "/Batches/Initiate"),
            ("/api/batches/B-TEST/steps/1/start", "/Batches/Details?number=B-TEST&handler=StartStep"),
            ($"/api/batches/B-TEST/steps/{forged.executionId}/complete", "/Batches/Details?number=B-TEST&handler=CompleteStep"),
            ("/api/batches/B-TEST/deviations", "/Batches/Details?number=B-TEST&handler=Deviation")
        })
        {
            await HttpManufacturing.Json(client, api + "?actorId=demo.operator&role=Operator", forged, 403);
            await HttpManufacturing.Form(client, ui + (ui.Contains('?') ? "&" : "?") + "actorId=demo.operator&role=Operator", forged, 403);
            Assert.Equal(before, await Snapshot(f));
        }
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task AntiforgeryAndRevocation_BlockBeforeMutationOnBothAdapters(string environment)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        await using var host = HttpManufacturing.Factory(f, environment);
        using var actor = await HttpManufacturing.Login(host, SecuredServiceFixture.Operator);
        using var other = await HttpManufacturing.Login(host, SecuredServiceFixture.MultiRole);
        var before = await Snapshot(f);
        var body = new { deviationCode = "D-TOKEN", description = "Must not persist" };
        const string route = "/api/batches/B-TEST/deviations";
        using var missing = await actor.PostAsJsonAsync(route, body);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType!.MediaType);
        await HttpManufacturing.Json(actor, route, body, 400, "invalid-token");
        await HttpManufacturing.Json(actor, route, body, 400, await HttpManufacturing.Token(other));
        using var missingForm = await actor.PostAsync("/Batches/Details?number=B-TEST&handler=Deviation", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, missingForm.StatusCode);
        Assert.Equal(before, await Snapshot(f));
        var validToken = await HttpManufacturing.Token(actor);
        await using (var scope = f.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.AppUsers.SingleAsync(u => u.UserId == SecuredServiceFixture.Operator)).IsActive = false;
            await db.SaveChangesAsync();
        }
        var denied = await actor.GetAsync("/api/production-orders/PO-TEST");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.Null(denied.Headers.Location);
        await HttpManufacturing.Json(actor, route, body, 401, validToken);
        Assert.Equal(HttpStatusCode.Redirect, (await actor.GetAsync("/Batches/Details?number=B-TEST")).StatusCode);
        using var post = await actor.PostAsync("/Batches/Details?number=B-TEST&handler=Deviation", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = validToken, ["deviationCode"] = "D-REVOKED", ["description"] = "Denied" }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal(before, await Snapshot(f));
    }

    private static async Task<string> Snapshot(SecuredServiceFixture f)
    {
        var view = await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAsync("B-TEST"));
        var audit = await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAuditAsync("B-TEST"));
        return JsonSerializer.Serialize(new { view.Value, Audit = audit.Value });
    }
}
