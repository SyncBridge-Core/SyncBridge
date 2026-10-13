using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Assembly;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Repositories;

internal sealed class ProductionOrderRepository(SyncBridgeDbContext context)
    : IProductionOrderRepository
{
    public async Task<ProductionOrder?> GetByNumberAsync(
        ProductionOrderNumber productionOrderNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productionOrderNumber);
        var row = await context.ProductionOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.OrderNumber == productionOrderNumber.Value,
                cancellationToken);

        return row is null ? null : ProductionOrderAssembler.Assemble(row);
    }

    public Task<bool> ExistsAsync(
        ProductionOrderNumber productionOrderNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productionOrderNumber);
        return context.ProductionOrders.AnyAsync(
            candidate => candidate.OrderNumber == productionOrderNumber.Value,
            cancellationToken);
    }

    public async Task AddAsync(
        ProductionOrder productionOrder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(productionOrder);
        var row = new ProductionOrderRow
        {
            OrderId = productionOrder.ProductionOrderId.Value,
            OrderNumber = DbdsStringLengthValidator.RequireWithinLimit(
                productionOrder.ProductionOrderNumber.Value, 40, nameof(productionOrder)),
            ProductCode = DbdsStringLengthValidator.RequireWithinLimit(
                productionOrder.ProductCode.Value, 40, nameof(productionOrder)),
            ProductName = DbdsStringLengthValidator.RequireWithinLimit(
                productionOrder.ProductName, 160, nameof(productionOrder)),
            RecipeId = TechnicalGuidConverter.ToGuid(productionOrder.RecipeId.Value),
            Quantity = productionOrder.Quantity,
            Status = productionOrder.ProductionOrderStatus,
            CreatedAt = productionOrder.CreatedAt
        };

        await context.ProductionOrders.AddAsync(row, cancellationToken);
    }
}
