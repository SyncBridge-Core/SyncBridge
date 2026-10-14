using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence;

namespace SyncBridge.Core.Infrastructure.Identity;

/// <summary>Small deployment-configured identity store; credentials and roles are outside manufacturing tables.</summary>
public sealed class ManufacturingIdentityOptions
{
    public List<ManufacturingAccount> Accounts { get; set; } = [];
}

public sealed class ManufacturingAccount
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

public sealed class ManufacturingIdentity(IHttpContextAccessor http, SyncBridgeDbContext context,
    IOptionsMonitor<ManufacturingIdentityOptions> options, IPasswordHasher<ManufacturingAccount> hasher,
    ILogger<ManufacturingIdentity> logger) : IActorContext, ICredentialAuthenticator
{
    private AuthenticatedActor? validatedActor;
    public OperatorId ActorId => validatedActor?.ActorId ?? throw new InvalidOperationException("Resolve the trusted actor before use.");

    public async Task<AuthenticatedActor?> GetActorAsync(CancellationToken cancellationToken = default)
    {
        validatedActor = null;
        var principal = http.HttpContext?.User;
        var identities = principal?.Identities.Where(i => i.IsAuthenticated && i.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme).ToArray();
        if (identities is not { Length: 1 }) return null;
        var identifiers = identities[0].FindAll(ClaimTypes.NameIdentifier).ToArray();
        if (identifiers.Length != 1) return null;
        var account = Account(a => a.UserId == identifiers[0].Value);
        if (account is null) return null;
        var user = await context.AppUsers.AsNoTracking().SingleOrDefaultAsync(row => row.UserId == account.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.UserId != account.UserId || user.UserName != account.UserName) return null;
        // Ignore caller-supplied role claims; use current trusted server configuration on every operation.
        validatedActor = new(new OperatorId(user.UserId), account.Roles.ToHashSet(StringComparer.Ordinal));
        return validatedActor;
    }

    public async Task<LoginIdentity?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || userName.Length > 100 || string.IsNullOrEmpty(password) || password.Length > 1024) return null;
        var account = Account(a => string.Equals(a.UserName, userName, StringComparison.OrdinalIgnoreCase));
        if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash)) return null;
        try
        {
            if (hasher.VerifyHashedPassword(account, account.PasswordHash, password) == PasswordVerificationResult.Failed) return null;
            var user = await context.AppUsers.AsNoTracking().SingleOrDefaultAsync(row => row.UserId == account.UserId, cancellationToken);
            return user is { IsActive: true } && user.UserId == account.UserId && user.UserName == account.UserName ? new(user.UserId, user.UserName) : null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Credential authentication could not be completed.");
            return null;
        }
    }

    private ManufacturingAccount? Account(Func<ManufacturingAccount, bool> predicate)
    {
        var accounts = options.CurrentValue.Accounts;
        var matches = accounts.Where(predicate).ToArray();
        if (matches.Length != 1) return null;
        var account = matches[0];
        if (string.IsNullOrWhiteSpace(account.UserId) || account.UserId.Length > 64 || string.IsNullOrWhiteSpace(account.UserName) || account.UserName.Length > 100 ||
            account.Roles.Any(role => !ManufacturingRoles.IsKnown(role)) || accounts.Count(a => a.UserId == account.UserId) != 1 ||
            accounts.Count(a => string.Equals(a.UserName, account.UserName, StringComparison.OrdinalIgnoreCase)) != 1) return null;
        return account;
    }
}
