using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Contracts.Persistence;

/// <summary>
/// Defines Batch aggregate persistence operations required by execution use cases.
/// </summary>
public interface IBatchRepository
{
    /// <summary>
    /// Gets a Batch execution aggregate with its required execution and evidence associations.
    /// </summary>
    /// <param name="batchNumber">The Batch business number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching Batch aggregate, or null when none exists.</returns>
    Task<Batch?> GetExecutionAggregateAsync(
        BatchNumber batchNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new Batch aggregate to the current persistence scope.
    /// </summary>
    /// <param name="batch">The Batch to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(Batch batch, CancellationToken cancellationToken = default);
}
