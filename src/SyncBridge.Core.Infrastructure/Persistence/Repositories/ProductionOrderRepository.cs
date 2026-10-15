using Microsoft.EntityFrameworkCore;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Assembly;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Exceptions;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Repositories;

internal sealed class ProductionOrderRepository(SyncBridgeDbContext context)
    : IProductionOrderRepository
{
    public async Task<IReadOnlyList<ProductionOrderNumber>> GetNumbersAsync(CancellationToken cancellationToken = default)
    {
        var numbers = await context.ProductionOrders.AsNoTracking().OrderBy(row => row.OrderNumber).Select(row => row.OrderNumber).ToArrayAsync(cancellationToken);
        try { return numbers.Select(number => new ProductionOrderNumber(DbdsStringLengthValidator.RequireWithinLimit(number, 40, nameof(number)))).ToArray(); }
        catch (Exception exception) when (exception is DomainException or ArgumentException)
        { throw new PersistenceIntegrityException("A persisted ProductionOrder identifier is invalid.", exception); }
    }

    public async Task UpdateAsync(ProductionOrder order, CancellationToken cancellationToken = default)
    {
        var row = await context.ProductionOrders.SingleAsync(item => item.OrderId == order.ProductionOrderId.Value, cancellationToken);
        row.Status = order.ProductionOrderStatus;
    }

    public Task AppendAuditAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) =>
        AuditPersistence.AppendAsync(context, auditEvent, null, cancellationToken);

    public async Task<IReadOnlyList<AuditEvent>> GetAuditAsync(ProductionOrderNumber number, CancellationToken cancellationToken = default)
    {
        var rows = await context.AuditEvents.AsNoTracking().Where(row => row.BatchId == null && row.AffectedEntity == nameof(ProductionOrder) && row.AffectedEntityId == number.Value)
            .ToArrayAsync(cancellationToken);
        return rows.OrderBy(row => row.OccurredAt).Select(row => AuditPersistence.Read(row, null)).ToArray();
    }

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

        try
        {
            return row is null ? null : ProductionOrderAssembler.Assemble(row);
        }
        catch (Exception exception) when (exception is DomainException or ArgumentException)
        {
            throw new PersistenceIntegrityException("The persisted ProductionOrder is invalid.", exception);
        }
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
