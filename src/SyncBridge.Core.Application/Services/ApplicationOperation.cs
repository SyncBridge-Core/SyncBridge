using Microsoft.Extensions.Logging;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Exceptions;

namespace SyncBridge.Core.Application.Services;

/// <summary>Small shared operation boundary for authorization, results and command cleanup.</summary>
public sealed class ApplicationOperation(IActorContext actors, IApplicationUnitOfWork unitOfWork, ILogger<ApplicationOperation> logger)
{
    internal async Task<OperationResult<T>> RunAsync<T>(string[] roles, bool mutation,
        Func<AuthenticatedActor, CancellationToken, Task<T>> execute, CancellationToken cancellationToken)
    {
        var began = false;
        try
        {
            var actor = await RequireActorAsync(roles, cancellationToken);
            if (mutation)
            {
                await unitOfWork.BeginAsync(cancellationToken);
                began = true;
                // Check activation and role membership inside the command transaction as well.
                actor = await RequireActorAsync(roles, cancellationToken);
            }
            var value = await execute(actor, cancellationToken);
            if (mutation) await unitOfWork.CommitAsync(cancellationToken);
            return new(OperationOutcome.Success, value);
        }
        catch (OperationRejection rejection) { return new(rejection.Outcome, Message: rejection.Outcome.ToString()); }
        catch (ArgumentException) { return new(OperationOutcome.InvalidInput, Message: "Invalid input."); }
        catch (DomainException) { return new(OperationOutcome.BusinessRuleRejected, Message: "The operation was rejected by manufacturing rules."); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Controlled application operation failed.");
            return new(OperationOutcome.TechnicalFailure, Message: "The operation could not be completed.");
        }
        finally
        {
            if (began)
            {
                try { await unitOfWork.RollbackAsync(CancellationToken.None); }
                catch (Exception exception) { logger.LogError(exception, "Command cleanup failed; discard this service scope."); }
            }
        }
    }

    private async Task<AuthenticatedActor> RequireActorAsync(string[] roles, CancellationToken cancellationToken)
    {
        var actor = await actors.GetActorAsync(cancellationToken) ?? throw new OperationRejection(OperationOutcome.Unauthenticated);
        if (!roles.Any(actor.Roles.Contains)) throw new OperationRejection(OperationOutcome.Forbidden);
        return actor;
    }

    internal static T Require<T>(T? resource) where T : class => resource ?? throw new OperationRejection(OperationOutcome.NotFound);
    internal static void RequireRule(bool valid) { if (!valid) throw new OperationRejection(OperationOutcome.BusinessRuleRejected); }
    internal static string Text(string value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum) throw new ArgumentException("Invalid text.");
        return value;
    }
}
