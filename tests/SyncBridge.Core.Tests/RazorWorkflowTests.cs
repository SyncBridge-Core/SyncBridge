using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Tests;

/// <summary>Actual Razor rendering, model binding, cookies, antiforgery, services and repositories; no manufacturing mocks.</summary>
public sealed class RazorWorkflowTests
{
    [Fact]
    public async Task UC001To004_HttpWorkflow_PreservesPrerequisitesSignaturesResolutionResumeAndCompletedProtection()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var factory = Factory(fixture);
        using var supervisor = await Login(factory, SecuredServiceFixture.Supervisor);
        using var productionOperator = await Login(factory, SecuredServiceFixture.Operator);
        using var quality = await Login(factory, SecuredServiceFixture.Quality);

        Assert.Contains("Create production order", await supervisor.GetStringAsync("/"));
        Assert.DoesNotContain("Create production order", await productionOperator.GetStringAsync("/"));
        Assert.True((await productionOperator.GetAsync("/")).Headers.CacheControl!.NoStore);
        await Post(supervisor, "/Orders/Create", HttpStatusCode.Redirect,
            ("OrderNumber", "PO-HTTP"), ("ProductCode", "P-HTTP"), ("ProductName", "Fictional HTTP product"), ("RecipeCode", "RCP-DEMO-001"), ("Quantity", "12.125"));
        await Post(productionOperator, "/Batches/Initiate", HttpStatusCode.UnprocessableEntity, ("OrderNumber", "PO-HTTP"), ("BatchNumber", "B-HTTP"));
        await Post(supervisor, "/Orders/Details?number=PO-HTTP&handler=Status", HttpStatusCode.Redirect, ("action", "Release"));
        await Post(productionOperator, "/Batches/Initiate", HttpStatusCode.Redirect, ("OrderNumber", "PO-HTTP"), ("BatchNumber", "B-HTTP"));
        const string batch = "/Batches/Details?number=B-HTTP";
        Assert.Contains("B-HTTP", await productionOperator.GetStringAsync("/"));
        Assert.Contains("MAT-DEMO-001", await productionOperator.GetStringAsync(batch));
        await Post(productionOperator, batch + "&handler=Ready", HttpStatusCode.Redirect);
        await Post(productionOperator, batch + "&handler=Start", HttpStatusCode.UnprocessableEntity);
        Assert.Contains("B-HTTP", await quality.GetStringAsync("/Review"));
        await Post(productionOperator, batch + "&handler=Material", HttpStatusCode.Redirect, ("itemCode", "MAT-DEMO-001"), ("result", "Failed"));
        await Post(productionOperator, batch + "&handler=Equipment", HttpStatusCode.Redirect, ("itemCode", "EQ-DEMO-001"), ("result", "Passed"));
        await Post(productionOperator, batch + "&handler=Start", HttpStatusCode.UnprocessableEntity);
        await Post(productionOperator, batch + "&handler=Material", HttpStatusCode.Redirect, ("itemCode", "MAT-DEMO-001"), ("result", "Passed"));
        await Post(productionOperator, batch + "&handler=Start", HttpStatusCode.Redirect);
        await Post(productionOperator, batch + "&handler=StartStep", HttpStatusCode.UnprocessableEntity, ("stepNumber", "2"));
        await Post(productionOperator, batch + "&handler=StartStep", HttpStatusCode.Redirect, ("stepNumber", "1"));
        var first = (await Read(fixture, "B-HTTP")).Steps.Single(s => s.StepNumber == 1);
        await Post(productionOperator, batch + "&handler=Deviation", HttpStatusCode.Redirect,
            ("deviationCode", "D-HTTP"), ("description", "Fictional deviation <script>alert(1)</script>"), ("executionId", first.ExecutionId));
        Assert.Contains("&lt;script&gt;", await quality.GetStringAsync(batch));
        await Post(productionOperator, batch + "&handler=Resume", HttpStatusCode.UnprocessableEntity);
        await Post(productionOperator, batch + "&handler=CompleteStep", HttpStatusCode.UnprocessableEntity, ("executionId", first.ExecutionId));
        await Post(quality, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-HTTP"), ("action", "BeginReview"), ("commentsOrDisposition", "Investigated"));
        await Post(quality, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-HTTP"), ("action", "Approve"));
        await Post(quality, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-HTTP"), ("action", "Resolve"), ("commentsOrDisposition", "Approved disposition"));
        Assert.Equal(BatchStatus.Exception, (await Read(fixture, "B-HTTP")).Status);
        await Post(quality, batch + "&handler=Resume", HttpStatusCode.Forbidden);
        await Post(supervisor, batch + "&handler=Complete", HttpStatusCode.UnprocessableEntity);
        await Post(productionOperator, batch + "&handler=Resume", HttpStatusCode.Redirect);
        await Post(productionOperator, batch + "&handler=CompleteStep", HttpStatusCode.Redirect, ("executionId", first.ExecutionId), ("recordedData", "First-step evidence"));
        await Post(productionOperator, batch + "&handler=StartStep", HttpStatusCode.Redirect, ("stepNumber", "2"));
        var second = (await Read(fixture, "B-HTTP")).Steps.Single(s => s.StepNumber == 2);
        Assert.True(second.RequiresSignature);
        await Post(productionOperator, batch + "&handler=CompleteStep", HttpStatusCode.UnprocessableEntity, ("executionId", second.ExecutionId));
        await Post(quality, batch + "&handler=Sign", HttpStatusCode.NotFound, ("executionId", Guid.NewGuid().ToString("D")), ("meaning", "Approval"));
        await Post(quality, batch + "&handler=Sign", HttpStatusCode.Redirect, ("executionId", second.ExecutionId), ("meaning", "Approval"));
        var signature = (await Read(fixture, "B-HTTP")).Signatures.Single();
        Assert.Equal(second.ExecutionId, signature.ExecutionId);
        Assert.Equal(SecuredServiceFixture.Quality, signature.ActorId);
        await Post(productionOperator, batch + "&handler=CompleteStep", HttpStatusCode.Redirect,
            ("executionId", second.ExecutionId), ("signatureId", signature.SignatureId), ("recordedData", "Signed evidence"));
        await Post(supervisor, batch + "&handler=Complete", HttpStatusCode.Redirect);
        var completed = await productionOperator.GetStringAsync(batch);
        Assert.Contains("read-only", completed);
        Assert.DoesNotContain("handler=Sign", completed);
        Assert.DoesNotContain("handler=Deviation", completed);
        Assert.DoesNotContain("handler=Complete", completed);
        await Post(productionOperator, batch + "&handler=Deviation", HttpStatusCode.UnprocessableEntity, ("deviationCode", "D-LATE"), ("description", "Forbidden late update"));
        await Post(quality, batch + "&handler=Sign", HttpStatusCode.UnprocessableEntity, ("executionId", second.ExecutionId), ("meaning", "Approval"));
        Assert.Contains("DeviationResolved", await quality.GetStringAsync("/Audit?batchNumber=B-HTTP"));
        Assert.Contains("ProductionOrderCreated", await supervisor.GetStringAsync("/Audit?orderNumber=PO-HTTP"));
        Assert.Equal(HttpStatusCode.Forbidden, (await productionOperator.GetAsync("/Audit?batchNumber=B-HTTP")).StatusCode);
        var final = await Read(fixture, "B-HTTP");
        Assert.Equal(BatchStatus.Completed, final.Status);
        Assert.Single(final.Deviations);
        Assert.Single(final.Signatures);
        Assert.Equal("Signed evidence", final.Steps.Single(s => s.StepNumber == 2).RecordedData);
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Operator, "/Orders/Details?number=PO-TEST&handler=Status", "action", "Release")]
    [InlineData(SecuredServiceFixture.Quality, "/Orders/Details?number=PO-TEST&handler=Status", "action", "Release")]
    [InlineData(SecuredServiceFixture.Administrator, "/Orders/Details?number=PO-TEST&handler=Status", "action", "Release")]
    [InlineData(SecuredServiceFixture.Supervisor, "/Batches/Details?number=B-TEST&handler=Ready", "", "")]
    [InlineData(SecuredServiceFixture.Quality, "/Batches/Details?number=B-TEST&handler=Start", "", "")]
    [InlineData(SecuredServiceFixture.Supervisor, "/Batches/Details?number=B-TEST&handler=Material", "itemCode", "MAT-DEMO-001")]
    [InlineData(SecuredServiceFixture.Quality, "/Batches/Details?number=B-TEST&handler=Equipment", "itemCode", "EQ-DEMO-001")]
    [InlineData(SecuredServiceFixture.Supervisor, "/Batches/Details?number=B-TEST&handler=StartStep", "stepNumber", "1")]
    [InlineData(SecuredServiceFixture.Quality, "/Batches/Details?number=B-TEST&handler=CompleteStep", "executionId", "00000000-0000-0000-0000-000000000001")]
    [InlineData(SecuredServiceFixture.Supervisor, "/Batches/Details?number=B-TEST&handler=Sign", "executionId", "00000000-0000-0000-0000-000000000001")]
    [InlineData(SecuredServiceFixture.Quality, "/Batches/Details?number=B-TEST&handler=Deviation", "description", "Unauthorized")]
    [InlineData(SecuredServiceFixture.Operator, "/Batches/Details?number=B-TEST&handler=Review", "deviationCode", "D-TEST")]
    [InlineData(SecuredServiceFixture.Supervisor, "/Batches/Details?number=B-TEST&handler=Review", "deviationCode", "D-TEST")]
    [InlineData(SecuredServiceFixture.Quality, "/Batches/Details?number=B-TEST&handler=Resume", "", "")]
    [InlineData(SecuredServiceFixture.Operator, "/Batches/Details?number=B-TEST&handler=Complete", "", "")]
    [InlineData(SecuredServiceFixture.Administrator, "/Batches/Details?number=B-TEST&handler=Complete", "", "")]
    public async Task DirectPost_ForbiddenRolesCannotSubstituteActorOrRole(string actor, string url, string field, string value)
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await fixture.PrepareAsync();
        await using var factory = Factory(fixture);
        using var client = await Login(factory, actor);
        await Post(client, url, HttpStatusCode.Forbidden, (field, value), ("result", "Passed"), ("meaning", "Approval"), ("action", url.Contains("handler=Review") ? "BeginReview" : "Release"),
            ("deviationCode", "D-TEST"), ("actorId", SecuredServiceFixture.Operator), ("role", "Production Supervisor"));
        var unchanged = await Read(fixture, "B-TEST");
        Assert.Equal(BatchStatus.Ready, unchanged.Status);
        Assert.Empty(unchanged.Steps);
        Assert.Empty(unchanged.Deviations);
        Assert.Empty(unchanged.Signatures);
        Assert.Empty(unchanged.Materials);
        Assert.Empty(unchanged.Equipment);
    }

    [Theory]
    [InlineData(SecuredServiceFixture.Operator)]
    [InlineData(SecuredServiceFixture.Quality)]
    [InlineData(SecuredServiceFixture.Administrator)]
    public async Task DirectCreationPosts_AreForbiddenForNonSupervisors(string actor)
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var factory = Factory(fixture);
        using var client = await Login(factory, actor);
        await Post(client, "/Orders/Create", HttpStatusCode.Forbidden, ("OrderNumber", "PO-DENIED"));
        if (actor != SecuredServiceFixture.Operator)
            await Post(client, "/Batches/Initiate", HttpStatusCode.Forbidden, ("OrderNumber", "PO-DEMO-001"), ("BatchNumber", "B-DENIED"));
    }

    [Fact]
    public async Task AdditiveRoles_CanReviewOwnDeviationAndResumeButCannotManageOrders()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await fixture.StartAsync();
        await using var factory = Factory(fixture);
        using var client = await Login(factory, SecuredServiceFixture.MultiRole);
        var html = await client.GetStringAsync("/");
        Assert.Contains("Review by Exception", html);
        Assert.DoesNotContain("Create production order", html);
        const string batch = "/Batches/Details?number=B-TEST";
        await Post(client, batch + "&handler=Deviation", HttpStatusCode.Redirect, ("deviationCode", "D-OWN"), ("description", "Own deviation"));
        await Post(client, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-OWN"), ("action", "BeginReview"), ("commentsOrDisposition", "Own review"));
        await Post(client, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-OWN"), ("action", "Approve"));
        await Post(client, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-OWN"), ("action", "Resolve"), ("commentsOrDisposition", "Resolved"));
        Assert.Equal(BatchStatus.Exception, (await Read(fixture, "B-TEST")).Status);
        await Post(client, batch + "&handler=Resume", HttpStatusCode.Redirect);
        Assert.Equal(BatchStatus.InProgress, (await Read(fixture, "B-TEST")).Status);
        await Post(client, "/Orders/Details?number=PO-TEST&handler=Status", HttpStatusCode.Forbidden, ("action", "StartProcessing"));
    }

    [Fact]
    public async Task InvalidBindingAntiforgeryAndTechnicalFailure_ReturnMeaningfulFeedbackWithoutMutation()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await fixture.PrepareAsync();
        await using var factory = Factory(fixture);
        using var client = await Login(factory, SecuredServiceFixture.Operator);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Batches/Details?number=B-TEST&handler=Start", new FormUrlEncodedContent([]))).StatusCode);
        await Post(client, "/Batches/Details?number=B-TEST&handler=Material", HttpStatusCode.UnprocessableEntity, ("itemCode", "MAT-DEMO-001"));
        await Post(client, "/Batches/Details?number=B-TEST&handler=Material", HttpStatusCode.UnprocessableEntity, ("itemCode", "MAT-DEMO-001"), ("result", "999"));
        await Post(client, "/Batches/Details?number=B-TEST&handler=StartStep", HttpStatusCode.UnprocessableEntity, ("stepNumber", "not-an-integer"));
        fixture.FailAfterFirstWrite();
        var failure = await Post(client, "/Batches/Details?number=B-TEST&handler=Material", HttpStatusCode.ServiceUnavailable, ("itemCode", "MAT-DEMO-001"), ("result", "Passed"));
        Assert.DoesNotContain("private diagnostic", failure);
        Assert.Contains("Try again later", failure);
        Assert.Empty((await Read(fixture, "B-TEST")).Materials);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Batches/Details?number=B-MISSING")).StatusCode);
    }

    [Fact]
    public async Task QualityRejection_RemainsBlockingAndAdministratorCannotReadManufacturingRecords()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await fixture.StartAsync();
        await using var factory = Factory(fixture);
        using var productionOperator = await Login(factory, SecuredServiceFixture.Operator);
        using var quality = await Login(factory, SecuredServiceFixture.Quality);
        using var admin = await Login(factory, SecuredServiceFixture.Administrator);
        const string batch = "/Batches/Details?number=B-TEST";
        await Post(productionOperator, batch + "&handler=Deviation", HttpStatusCode.Redirect, ("deviationCode", "D-REJECT"), ("description", "Reject case"));
        await Post(quality, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-REJECT"), ("action", "BeginReview"), ("commentsOrDisposition", "Reject review"));
        await Post(quality, batch + "&handler=Review", HttpStatusCode.Redirect, ("deviationCode", "D-REJECT"), ("action", "Reject"));
        await Post(quality, batch + "&handler=Review", HttpStatusCode.UnprocessableEntity, ("deviationCode", "D-REJECT"), ("action", "Resolve"), ("commentsOrDisposition", "Invalid resolution"));
        await Post(productionOperator, batch + "&handler=Resume", HttpStatusCode.UnprocessableEntity);
        Assert.Contains("no manufacturing permissions", await admin.GetStringAsync("/"));
        foreach (var url in new[] { batch, "/Orders/Details?number=PO-TEST", "/Review", "/Audit?batchNumber=B-TEST" })
            Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task MissingAndRevokedCookie_CannotAccessPagesOrPostCommands()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await fixture.PrepareAsync();
        await using var factory = Factory(fixture);
        using var anonymous = Client(factory);
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/")).StatusCode);
        await Post(anonymous, "/Batches/Details?number=B-TEST&handler=Start", HttpStatusCode.Redirect);
        using var client = await Login(factory, SecuredServiceFixture.Operator);
        var token = await Token(client);
        await using (var scope = fixture.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.AppUsers.SingleAsync(u => u.UserId == SecuredServiceFixture.Operator)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Batches/Details?number=B-TEST&handler=Start", new FormUrlEncodedContent([new("__RequestVerificationToken", token)]))).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CorruptDashboardIdentifier_IsTechnicalFailureRatherThanCallerValidation(bool batchIdentifier)
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await fixture.PrepareAsync();
        await using var factory = Factory(fixture);
        using var client = await Login(factory, SecuredServiceFixture.Operator);
        await using (var scope = fixture.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            if (batchIdentifier) (await db.Batches.SingleAsync()).BatchNumber = " ";
            else (await db.ProductionOrders.SingleAsync(o => o.OrderNumber == "PO-TEST")).OrderNumber = " ";
            await db.SaveChangesAsync();
        }
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("persisted", await response.Content.ReadAsStringAsync());
    }

    private static async Task<BatchView> Read(SecuredServiceFixture fixture, string number) =>
        (await fixture.Batch(SecuredServiceFixture.Supervisor, service => service.GetAsync(number))).Value!;
    private static WebApplicationFactory<Program> Factory(SecuredServiceFixture fixture) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("ConnectionStrings:SyncBridgeCore", "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True");
        builder.ConfigureServices(fixture.ConfigureTestHost);
    });
    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    private static async Task<HttpClient> Login(WebApplicationFactory<Program> factory, string actor)
    {
        var client = Client(factory);
        await Post(client, "/authentication/login", HttpStatusCode.Redirect, ("userName", actor), ("password", SecuredServiceFixture.Password));
        return client;
    }
    private static async Task<string> Token(HttpClient client)
    {
        var html = await client.GetStringAsync("/authentication");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static async Task<string> Post(HttpClient client, string url, HttpStatusCode status, params (string Name, string Value)[] fields)
    {
        var form = fields.Where(f => f.Name != "").GroupBy(f => f.Name).Select(g => new KeyValuePair<string, string>(g.Key, g.First().Value)).ToList();
        form.Add(new("__RequestVerificationToken", await Token(client)));
        var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{url}: expected {status}, got {response.StatusCode}. {body[..Math.Min(body.Length, 1000)]}");
        return body;
    }
}
