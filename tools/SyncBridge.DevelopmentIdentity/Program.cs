using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Infrastructure;
using SyncBridge.Core.Infrastructure.Identity;
using SyncBridge.Core.Infrastructure.Persistence;

// Local setup utility only: no HTTP endpoint, database writes, or production authentication changes.
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development" || args.Length == 0)
{
    Console.Error.WriteLine("Set ASPNETCORE_ENVIRONMENT=Development. Use --provision <approved-manifest.json> or --verify. Passwords use hidden prompts by default; --generate-passwords provisions random local development passwords, --stored-development-passwords verifies those privately stored passwords, and --passwords-stdin accepts a JSON map.");
    return 1;
}
try
{
    var projectDirectory = Path.GetFullPath("src/SyncBridge.Core");
    var projectFile = Path.Combine(projectDirectory, "SyncBridge.Core.csproj");
    var secretId = XDocument.Load(projectFile).Descendants("UserSecretsId").Single().Value;
    var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(projectDirectory, "appsettings.json"))
        .AddJsonFile(Path.Combine(projectDirectory, "appsettings.Development.json"), optional: true)
        .AddUserSecrets(secretId).AddEnvironmentVariables().Build();
    var services = new ServiceCollection().AddLogging();
    ApplicationComposition.Configure(services, configuration);
    await using var provider = services.BuildServiceProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<SyncBridgeDbContext>();
    var provisioning = args[0] == "--provision";
    if (!provisioning && args[0] != "--verify") throw new InvalidOperationException();
    var accounts = provisioning
        ? JsonSerializer.Deserialize<ManufacturingAccount[]>(await File.ReadAllTextAsync(args[1]))!
        : configuration.GetSection("ManufacturingIdentity:Accounts").Get<ManufacturingAccount[]>() ?? [];
    if (accounts.Length == 0 || accounts.Any(a => a.Roles.Count == 0 || a.Roles.Any(r => !ManufacturingRoles.IsKnown(r))) ||
        accounts.Select(a => a.UserId).Distinct(StringComparer.Ordinal).Count() != accounts.Length ||
        accounts.Select(a => a.UserName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != accounts.Length)
        throw new InvalidOperationException();
    // Verify every association before prompting or writing any credentials. Never create users here.
    foreach (var account in accounts)
    {
        var user = await db.AppUsers.AsNoTracking().SingleOrDefaultAsync(u => u.UserId == account.UserId);
        if (user is not { IsActive: true } || user.UserId != account.UserId || user.UserName != account.UserName)
            throw new InvalidOperationException();
    }
    var configured = configuration.GetSection("ManufacturingIdentity:Accounts").Get<ManufacturingAccount[]>() ?? [];
    if (provisioning && configured.Length > 0 && (configured.Length != accounts.Length ||
        configured.Where((a, i) => a.UserId != accounts[i].UserId || a.UserName != accounts[i].UserName || !a.Roles.ToHashSet().SetEquals(accounts[i].Roles)).Any()))
        throw new InvalidOperationException(); // Roster changes require explicit identity-section maintenance; no stale roles retained.
    var supplied = args.Contains("--passwords-stdin")
        ? JsonSerializer.Deserialize<Dictionary<string, string>>(await Console.In.ReadToEndAsync())! : null;
    var generated = provisioning && args.Contains("--generate-passwords");
    var passwords = accounts.ToDictionary(a => a.UserId, a => generated ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
        : args.Contains("--stored-development-passwords") ? configuration[$"DevelopmentIdentity:Passwords:{a.UserId}"]!
        : supplied is null ? ReadPassword(a.UserName) : supplied[a.UserId]);
    if (passwords.Values.Any(p => string.IsNullOrEmpty(p) || p.Length > 1024)) throw new InvalidOperationException();
    if (provisioning)
    {
        var settings = new Dictionary<string, string>();
        for (var i = 0; i < accounts.Length; i++)
        {
            var account = accounts[i];
            var key = $"ManufacturingIdentity:Accounts:{i}";
            settings[$"{key}:UserId"] = account.UserId;
            settings[$"{key}:UserName"] = account.UserName;
            settings[$"{key}:PasswordHash"] = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ManufacturingAccount>>().HashPassword(account, passwords[account.UserId]);
            if (generated) settings[$"DevelopmentIdentity:Passwords:{account.UserId}"] = passwords[account.UserId];
            for (var j = 0; j < account.Roles.Count; j++) settings[$"{key}:Roles:{j}"] = account.Roles[j];
        }
        var start = new ProcessStartInfo("dotnet") { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in new[] { "user-secrets", "set", "--project", projectFile }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(settings));
        process.StandardInput.Close();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await Task.WhenAll(outputTask, errorTask); // Consume without emitting hashes or sensitive diagnostics.
        if (process.ExitCode != 0) throw new InvalidOperationException();
        Console.WriteLine($"Provisioned {accounts.Length} approved active identities into the web project's User Secrets. Run --verify, then restart the Development host.");
    }
    else
    {
        var authenticator = scope.ServiceProvider.GetRequiredService<ICredentialAuthenticator>();
        var http = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        foreach (var account in accounts)
        {
            var identity = await authenticator.AuthenticateAsync(account.UserName, passwords[account.UserId]);
            if (identity?.UserId != account.UserId) throw new InvalidOperationException();
            http.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, identity.UserId)], CookieAuthenticationDefaults.AuthenticationScheme)) };
            var actor = await scope.ServiceProvider.GetRequiredService<IActorContext>().GetActorAsync();
            if (actor is null || !actor.Roles.SetEquals(account.Roles)) throw new InvalidOperationException();
            Console.WriteLine($"Verified active identity {account.UserName}: {string.Join(", ", actor.Roles.Order())}.");
        }
    }
    return 0;
}
catch
{
    Console.Error.WriteLine("Development identity setup failed. Check the approved manifest, active AppUser associations, connection configuration, passwords, environment overrides and existing identity section. No credentials are printed.");
    return 1;
}

static string ReadPassword(string userName)
{
    if (Console.IsInputRedirected) throw new InvalidOperationException();
    Console.Write($"Development password for {userName} (hidden): ");
    var password = new System.Text.StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return password.ToString(); }
        if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
        else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
    }
}
