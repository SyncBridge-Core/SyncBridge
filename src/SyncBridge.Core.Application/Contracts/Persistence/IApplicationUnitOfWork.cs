namespace SyncBridge.Core.Application.Contracts.Persistence;

/// <summary>
/// Defines the atomic commit boundary for one controlled application command.
/// </summary>
public interface IApplicationUnitOfWork
{
    /// <summary>Begins an isolated controlled command; concurrent changes must not bypass gates.</summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back and discards tracked command changes, including after cancellation.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits all tracked changes for the current controlled command as one unit.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of persisted state entries.</returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
