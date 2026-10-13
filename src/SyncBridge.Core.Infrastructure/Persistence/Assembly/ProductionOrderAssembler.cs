using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Rows;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Infrastructure.Persistence.Assembly;

internal static class ProductionOrderAssembler
{
    internal static ProductionOrder Assemble(ProductionOrderRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return ProductionOrder.Rehydrate(
            new ProductionOrderId(row.OrderId),
            new ProductionOrderNumber(
                DbdsStringLengthValidator.RequireWithinLimit(row.OrderNumber, 40, nameof(row.OrderNumber))),
            new ProductCode(
                DbdsStringLengthValidator.RequireWithinLimit(row.ProductCode, 40, nameof(row.ProductCode))),
            DbdsStringLengthValidator.RequireWithinLimit(row.ProductName, 160, nameof(row.ProductName)),
            new RecipeId(TechnicalGuidConverter.ToCanonicalString(row.RecipeId)),
            row.Quantity,
            row.Status,
            row.CreatedAt);
    }
}
