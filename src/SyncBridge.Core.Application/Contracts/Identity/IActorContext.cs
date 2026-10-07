using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Contracts.Identity;

/// <summary>
/// Provides the trusted actor identity associated with the current application operation.
/// </summary>
public interface IActorContext
{
    /// <summary>
    /// Gets the responsible actor identity supplied to controlled Domain operations.
    /// </summary>
    OperatorId ActorId { get; }
}
