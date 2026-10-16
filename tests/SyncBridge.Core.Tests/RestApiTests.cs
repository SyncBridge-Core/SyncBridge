using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Tests;

public sealed class RestApiTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task UnhandledFault_IsGenericJsonInEveryEnvironment(string environment)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var host = Factory(f).WithWebHostBuilder(b =>
        {
            b.UseSetting("environment", environment);
            b.ConfigureServices(services => services.AddScoped<ProductionOrderService>(_ =>
                throw new InvalidOperationException("private diagnostic")));
        });
        using var client = await Login(host, SecuredServiceFixture.Operator);
        var response = await client.GetAsync("/api/production-orders/PO-DEMO-001");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("private diagnostic", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/production-orders/PO-DEMO-001", false)]
    [InlineData("/api/batches", true)]
    [InlineData("/api/batches/B-TEST/steps/1/start", true)]
    [InlineData("/api/batches/B-TEST/steps/id/complete", true)]
    [InlineData("/api/batches/B-TEST/deviations", true)]
    public async Task Anonymous_Is401WithoutRedirect(string route, bool post)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var host = Factory(f);
        using var client = Client(host);
        var response = post ? await client.PostAsJsonAsync(route, new { }) : await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Supervisor)]
    [InlineData(SecuredServiceFixture.Quality)]
    [InlineData(SecuredServiceFixture.Administrator)]
    public async Task NonOperators_CannotForgeMutationAuthority(string actor)
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        await using var host = Factory(f);
        using var client = await Login(host, actor);
        foreach (var route in new[] { "/api/batches", "/api/batches/B-TEST/steps/1/start", "/api/batches/B-TEST/steps/id/complete", "/api/batches/B-TEST/deviations" })
            await Post(client, route, new { orderNumber = "PO-TEST", batchNumber = "B-FORGED", deviationCode = "D-FORGED", description = "Forged", actorId = SecuredServiceFixture.Operator, role = "Operator" }, HttpStatusCode.Forbidden);
        var batch = await Read(f);
        Assert.Empty(batch.Steps);
        Assert.Empty(batch.Deviations);
    }

    [Fact]
    public async Task ContextAndInitiation_ValidateInputEligibilityTrustedActorAndAdditiveRoles()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await using var host = Factory(f);
        using var client = await Login(host, SecuredServiceFixture.MultiRole);
        var order = await client.GetFromJsonAsync<ProductionOrderView>("/api/production-orders/PO-DEMO-001");
        Assert.Equal("PO-DEMO-001", order!.OrderNumber);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/production-orders/MISSING")).StatusCode);
        using var admin = await Login(host, SecuredServiceFixture.Administrator);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/production-orders/PO-DEMO-001")).StatusCode);
        await Post(client, "/api/batches", new { }, HttpStatusCode.BadRequest);
        await f.Order(SecuredServiceFixture.Supervisor, s => s.CreateAsync(new("PO-NEW", "P", "Product", "RCP-DEMO-001", 1)));
        await Post(client, "/api/batches", new { orderNumber = "PO-NEW", batchNumber = "B-TEST" }, HttpStatusCode.UnprocessableEntity);
        await f.Order(SecuredServiceFixture.Supervisor, s => s.ChangeStatusAsync("PO-NEW", ProductionOrderAction.Release));
        var created = await Post(client, "/api/batches", new { orderNumber = "PO-NEW", batchNumber = "B-TEST", actorId = SecuredServiceFixture.Supervisor }, HttpStatusCode.Created);
        Assert.Equal(BatchStatus.Created, (await created.Content.ReadFromJsonAsync<BatchView>())!.Status);
        var audit = (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAuditAsync("B-TEST"))).Value!;
        Assert.NotEmpty(audit);
        Assert.All(audit, a => Assert.Equal(SecuredServiceFixture.MultiRole, a.ActorId));
        await Post(client, "/api/batches", new { orderNumber = "PO-NEW", batchNumber = "B-TEST" }, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task StepWorkflow_PreservesOrderApprovalAuditCompletedProtectionAndDeviationOrigin()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.StartAsync();
        await using var host = Factory(f);
        using var client = await Login(host, SecuredServiceFixture.Operator);
        await Post(client, "/api/batches/B-TEST/steps/2/start", new { }, HttpStatusCode.UnprocessableEntity);
        await Post(client, "/api/batches/B-TEST/steps/1/start", new { }, HttpStatusCode.OK);
        var first = (await Read(f)).Steps.Single();
        await Post(client, "/api/batches/B-TEST/deviations", new { deviationCode = "D-API", description = "API exception", executionId = first.ExecutionId, actorId = SecuredServiceFixture.Quality }, HttpStatusCode.OK);
        var exception = await Read(f);
        Assert.Equal(BatchStatus.Exception, exception.Status);
        Assert.Equal(first.ExecutionId, exception.Deviations.Single().ExecutionId);
        await Post(client, $"/api/batches/B-TEST/steps/{first.ExecutionId}/complete", new { }, HttpStatusCode.UnprocessableEntity);
        await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "D-API", DeviationAction.BeginReview, "Reviewed"));
        await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "D-API", DeviationAction.Approve));
        await f.Batch(SecuredServiceFixture.Quality, s => s.ReviewDeviationAsync("B-TEST", "D-API", DeviationAction.Resolve, "Resolved"));
        Assert.Equal(BatchStatus.Exception, (await Read(f)).Status);
        await f.Batch(SecuredServiceFixture.Operator, s => s.ResumeAsync("B-TEST"));
        await Post(client, $"/api/batches/B-TEST/steps/{first.ExecutionId}/complete", new { recordedData = "API evidence" }, HttpStatusCode.OK);
        await Post(client, "/api/batches/B-TEST/steps/2/start", new { }, HttpStatusCode.OK);
        var second = (await Read(f)).Steps.Single(s => s.StepNumber == 2);
        await Post(client, $"/api/batches/B-TEST/steps/{second.ExecutionId}/complete", new { }, HttpStatusCode.UnprocessableEntity);
        await f.Batch(SecuredServiceFixture.Quality, s => s.SignStepAsync("B-TEST", second.ExecutionId, ElectronicSignatureMeaning.Approval));
        var signature = (await Read(f)).Signatures.Single();
        await Post(client, $"/api/batches/B-TEST/steps/{second.ExecutionId}/complete", new { signatureId = signature.SignatureId }, HttpStatusCode.OK);
        await f.Batch(SecuredServiceFixture.Supervisor, s => s.CompleteAsync("B-TEST"));
        var before = (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAuditAsync("B-TEST"))).Value!.Count;
        await Post(client, "/api/batches/B-TEST/steps/1/start", new { }, HttpStatusCode.UnprocessableEntity);
        await Post(client, $"/api/batches/B-TEST/steps/{second.ExecutionId}/complete", new { signatureId = signature.SignatureId }, HttpStatusCode.UnprocessableEntity);
        await Post(client, "/api/batches/B-TEST/deviations", new { deviationCode = "D-LATE", description = "Late" }, HttpStatusCode.UnprocessableEntity);
        var final = await Read(f);
        Assert.Equal(BatchStatus.Completed, final.Status);
        Assert.Single(final.Signatures);
        Assert.Single(final.Deviations);
        Assert.Equal("API evidence", final.Steps.Single(s => s.StepNumber == 1).RecordedData);
        var audit = (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAuditAsync("B-TEST"))).Value!;
        Assert.Equal(before, audit.Count);
        Assert.Equal(audit.Count, audit.Select(a => a.Id).Distinct().Count());
        Assert.Contains(audit, a => a.Action.ToString().Contains("Deviation") && a.ActorId == SecuredServiceFixture.Operator);
    }

    [Fact]
    public async Task PrerequisitesAntiforgeryMalformedInputAndFaults_LeaveNoPartialEvidence()
    {
        await using var f = await SecuredServiceFixture.CreateAsync();
        await f.PrepareAsync();
        await using var host = Factory(f);
        using var client = await Login(host, SecuredServiceFixture.Operator);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/batches/B-TEST/steps/1/start", new { })).StatusCode);
        await Post(client, "/api/batches/B-TEST/steps/1/start", new { }, HttpStatusCode.UnprocessableEntity);
        await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Failed));
        await Post(client, "/api/batches/B-TEST/steps/1/start", new { }, HttpStatusCode.UnprocessableEntity);
        Assert.Empty((await Read(f)).Steps);
        await Post(client, "/api/batches/B-TEST/steps/not-guid/complete", new { }, HttpStatusCode.BadRequest);
        await Post(client, "/api/batches/B-TEST/deviations", new { deviationCode = "D", description = "Invalid association", executionId = Guid.NewGuid().ToString("D") }, HttpStatusCode.NotFound);
        await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyMaterialAsync("B-TEST", "MAT-DEMO-001", VerificationResult.Passed));
        await f.Batch(SecuredServiceFixture.Operator, s => s.VerifyEquipmentAsync("B-TEST", "EQ-DEMO-001", VerificationResult.Passed));
        await f.Batch(SecuredServiceFixture.Operator, s => s.StartProcessingAsync("B-TEST"));
        var before = (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAuditAsync("B-TEST"))).Value!.Count;
        f.FailAfterFirstWrite();
        var failure = await Post(client, "/api/batches/B-TEST/deviations", new { deviationCode = "D-FAIL", description = "Rollback" }, HttpStatusCode.ServiceUnavailable);
        Assert.DoesNotContain("private diagnostic", await failure.Content.ReadAsStringAsync());
        var batch = await Read(f);
        Assert.Equal(BatchStatus.InProgress, batch.Status);
        Assert.Empty(batch.Deviations);
        Assert.Equal(before, (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAuditAsync("B-TEST"))).Value!.Count);
        client.DefaultRequestHeaders.Remove("RequestVerificationToken");
        client.DefaultRequestHeaders.Add("RequestVerificationToken", await Token(client));
        var malformed = await client.PostAsync("/api/batches", new StringContent("{bad", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Contains("errors", await malformed.Content.ReadAsStringAsync());
    }

    private static async Task<BatchView> Read(SecuredServiceFixture f) => (await f.Batch(SecuredServiceFixture.Supervisor, s => s.GetAsync("B-TEST"))).Value!;
    private static WebApplicationFactory<Program> Factory(SecuredServiceFixture f) => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseSetting("ConnectionStrings:SyncBridgeCore", "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True");
        b.ConfigureServices(f.ConfigureTestHost);
    });
    private static HttpClient Client(WebApplicationFactory<Program> host) => host.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    private static async Task<string> Token(HttpClient client)
    {
        var html = await client.GetStringAsync("/authentication");
        return WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    private static async Task<HttpClient> Login(WebApplicationFactory<Program> host, string actor)
    {
        var client = Client(host);
        var response = await client.PostAsync("/authentication/login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["userName"] = actor, ["password"] = SecuredServiceFixture.Password, ["__RequestVerificationToken"] = await Token(client) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string url, object body, HttpStatusCode expected)
    {
        client.DefaultRequestHeaders.Remove("RequestVerificationToken");
        client.DefaultRequestHeaders.Add("RequestVerificationToken", await Token(client));
        var response = await client.PostAsJsonAsync(url, body);
        Assert.True(response.StatusCode == expected, $"{url}: {response.StatusCode}, expected {expected}");
        if (!response.IsSuccessStatusCode) Assert.Contains("json", response.Content.Headers.ContentType!.MediaType!);
        return response;
    }
}
