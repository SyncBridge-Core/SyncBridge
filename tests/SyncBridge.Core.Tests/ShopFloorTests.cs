using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Tests;

public sealed class ShopFloorTests
{
    [Theory]
    [InlineData(SecuredServiceFixture.Operator, HttpStatusCode.OK)]
    [InlineData(SecuredServiceFixture.MultiRole, HttpStatusCode.OK)]
    [InlineData(SecuredServiceFixture.Supervisor, HttpStatusCode.Forbidden)]
    [InlineData(SecuredServiceFixture.Quality, HttpStatusCode.Forbidden)]
    [InlineData(SecuredServiceFixture.Administrator, HttpStatusCode.Forbidden)]
    public async Task Page_EnforcesTrustedOperatorRole(string actor, HttpStatusCode expected)
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var host = Factory(fixture);
        using var client = Client(host);
        await Login(client, actor, "/shop-floor");
        var response = await client.GetAsync("/shop-floor");
        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.OK)
        {
            Assert.True(response.Headers.CacheControl!.NoStore);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("VECTRONIX", html);
            Assert.Contains("/api/production-orders/", html);
            Assert.DoesNotContain("PO-24001", html);
        }
        // Page restriction must not narrow the accepted REST policy.
        var api = await client.GetAsync("/api/production-orders/PO-DEMO-001");
        Assert.Equal(actor == SecuredServiceFixture.Administrator ? HttpStatusCode.Forbidden : HttpStatusCode.OK, api.StatusCode);
    }

    [Theory]
    [InlineData("/shop-floor", "/shop-floor")]
    [InlineData("/shop-floor?terminal=mx200", "/shop-floor?terminal=mx200")]
    [InlineData("https://attacker.invalid/", "/")]
    [InlineData("//attacker.invalid/", "/")]
    [InlineData("/\\attacker.invalid/", "/")]
    [InlineData("", "/")]
    public async Task Login_PreservesOnlySafeLocalReturnUrls(string returnUrl, string expected)
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var host = Factory(fixture);
        using var client = Client(host);
        var challenge = await client.GetAsync("/shop-floor");
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        Assert.Contains("/authentication?ReturnUrl=%2Fshop-floor", challenge.Headers.Location!.ToString());
        var response = await Login(client, SecuredServiceFixture.Operator, returnUrl);
        Assert.Equal(expected, response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/shop-floor")).StatusCode);
    }

    [Fact]
    public async Task HttpRetrieval_UsesPersistedDtoWithoutMutation_AndRevocationBlocksPage()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        const string product = "<img src=x onerror=alert(1)>";
        var created = await fixture.Order(SecuredServiceFixture.Supervisor,
            s => s.CreateAsync(new("PO-HMI", "HMI-PRODUCT", product, "RCP-DEMO-001", 12.125m)));
        Assert.True(created.Succeeded);
        await using var host = Factory(fixture);
        using var client = Client(host);
        await Login(client, SecuredServiceFixture.Operator, "/shop-floor");
        var read = await client.GetFromJsonAsync<ProductionOrderView>("/api/production-orders/PO-HMI");
        Assert.Equal(created.Value, read);
        var json = await client.GetStringAsync("/api/production-orders/PO-HMI");
        Assert.Contains("\"orderNumber\"", json);
        Assert.Contains("\"status\":0", json);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/production-orders/MISSING")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/production-orders/" + new string('X', 41))).StatusCode);
        await using (var scope = fixture.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            Assert.Single(await db.AuditEvents.ToListAsync()); // Only the scenario's order creation.
            (await db.AppUsers.SingleAsync(u => u.UserId == SecuredServiceFixture.Operator)).IsActive = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/shop-floor")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/production-orders/PO-HMI")).StatusCode);
    }

    private static WebApplicationFactory<Program> Factory(SecuredServiceFixture fixture) => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseSetting("ConnectionStrings:SyncBridgeCore", "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True");
        b.ConfigureServices(fixture.ConfigureTestHost);
    });
    private static HttpClient Client(WebApplicationFactory<Program> host) => host.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    private static async Task<HttpResponseMessage> Login(HttpClient client, string actor, string returnUrl)
    {
        var html = await client.GetStringAsync("/authentication?ReturnUrl=" + Uri.EscapeDataString(returnUrl));
        Assert.Contains("name=\"returnUrl\"", html);
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var response = await client.PostAsync("/authentication/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["userName"] = actor,
            ["password"] = SecuredServiceFixture.Password, ["returnUrl"] = returnUrl
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response;
    }
}
