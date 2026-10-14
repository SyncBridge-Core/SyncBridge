using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Contracts.Identity;

public sealed record AuthenticatedActor(OperatorId ActorId, IReadOnlySet<string> Roles);

public static class ManufacturingRoles
{
    public const string Operator = "Production Operator";
    public const string Supervisor = "Production Supervisor";
    public const string Quality = "Quality Engineer";
    public const string Administrator = "System Administrator";

    public static bool IsKnown(string role) => role is Operator or Supervisor or Quality or Administrator;
}

/// <summary>Credential authentication only; it cannot accept a caller-selected actor or role.</summary>
public interface ICredentialAuthenticator
{
    Task<LoginIdentity?> AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
}

public sealed record LoginIdentity(string UserId, string UserName);
