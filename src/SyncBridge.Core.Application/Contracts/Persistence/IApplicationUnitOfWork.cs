namespace SyncBridge.Core.Application.Contracts.Persistence;

/// <summary>
/// Defines the atomic commit boundary for one controlled application command.
/// </summary>
public interface IApplicationUnitOfWork
{
    /// <summary>
    /// Commits all tracked changes for the current controlled command as one unit.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of persisted state entries.</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
