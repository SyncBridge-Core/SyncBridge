using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Contracts.Persistence;

/// <summary>
/// Defines ProductionOrder persistence operations required by application use cases.
/// </summary>
public interface IProductionOrderRepository
{
    /// <summary>
    /// Gets a ProductionOrder by its business order number.
    /// </summary>
    /// <param name="productionOrderNumber">The production-order business number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching ProductionOrder, or null when none exists.</returns>
    Task<ProductionOrder?> GetByNumberAsync(
        ProductionOrderNumber productionOrderNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a ProductionOrder uses the supplied business number.
    /// </summary>
    /// <param name="productionOrderNumber">The production-order business number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the business number exists; otherwise false.</returns>
    Task<bool> ExistsAsync(
        ProductionOrderNumber productionOrderNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new ProductionOrder to the current persistence scope.
    /// </summary>
    /// <param name="productionOrder">The ProductionOrder to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddAsync(
        ProductionOrder productionOrder,
        CancellationToken cancellationToken = default);
}
