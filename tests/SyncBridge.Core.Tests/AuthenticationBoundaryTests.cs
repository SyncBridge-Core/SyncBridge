using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Tests;

public sealed class AuthenticationBoundaryTests
{
    [Fact]
    public async Task LoginLogout_RequireAntiforgeryAndIssueSecureCookieForValidatedUserOnly()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var factory = Factory(fixture);
        using var client = Client(factory);
        var noToken = await client.PostAsync("/authentication/login", new FormUrlEncodedContent([
            new("userName", SecuredServiceFixture.Operator), new("password", SecuredServiceFixture.Password)]));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var token = await Token(client);
        var badPassword = await client.PostAsync("/authentication/login", Form(token, "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, badPassword.StatusCode);
        Assert.False(badPassword.Headers.TryGetValues("Set-Cookie", out _));
        var login = await client.PostAsync("/authentication/login", Form(token, SecuredServiceFixture.Password));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-SyncBridge="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        var protectedValue = cookie.Split(';')[0].Split('=', 2)[1];
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = options.TicketDataFormat.Unprotect(protectedValue)!;
        Assert.Equal(SecuredServiceFixture.Operator, ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Empty(ticket.Principal.FindAll(ClaimTypes.Role));
        Assert.Equal(TimeSpan.FromMinutes(30), options.ExpireTimeSpan);
        Assert.False(options.SlidingExpiration);
        var missingLogoutToken = await client.PostAsync("/authentication/logout", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, missingLogoutToken.StatusCode);
        var authenticatedToken = await Token(client);
        var logout = await client.PostAsync("/authentication/logout", new FormUrlEncodedContent([new("__RequestVerificationToken", authenticatedToken)]));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
    }

    [Fact]
    public async Task InactiveUserCookie_IsRejectedOnTheNextRequest()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var factory = Factory(fixture);
        using var client = Client(factory);
        var token = await Token(client);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/authentication/login", Form(token, SecuredServiceFixture.Password))).StatusCode);
        await using (var scope = fixture.Scope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
            (await db.AppUsers.SingleAsync(u => u.UserId == SecuredServiceFixture.Operator)).IsActive = false;
            await db.SaveChangesAsync();
        }
        var rejected = await client.PostAsync("/authentication/logout", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
        Assert.StartsWith("https://localhost/authentication", rejected.Headers.Location!.ToString());
    }

    [Fact]
    public async Task LoginAttempts_AreRateLimited()
    {
        await using var fixture = await SecuredServiceFixture.CreateAsync();
        await using var factory = Factory(fixture);
        using var client = Client(factory);
        var token = await Token(client);
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/authentication/login", Form(token, "wrong"))).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/authentication/login", Form(token, "wrong"))).StatusCode);
    }

    private static WebApplicationFactory<Program> Factory(SecuredServiceFixture f) => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("ConnectionStrings:SyncBridgeCore", "Server=not-used.invalid;Database=SyncBridgeCore;Integrated Security=True");
        builder.ConfigureServices(f.ConfigureTestHost);
    });
    private static HttpClient Client(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    private static async Task<string> Token(HttpClient client)
    {
        var response = await client.GetAsync("/authentication");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static FormUrlEncodedContent Form(string token, string password) => new([
        new("__RequestVerificationToken", token), new("userName", SecuredServiceFixture.Operator), new("password", password),
        new("actorId", SecuredServiceFixture.Supervisor), new("role", "Production Supervisor")]);
}
