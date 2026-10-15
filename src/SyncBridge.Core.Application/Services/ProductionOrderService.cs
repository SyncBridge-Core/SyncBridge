using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Contracts.Persistence;
using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Application.Services;

public sealed class ProductionOrderService(ApplicationOperation operations, IProductionOrderRepository orders, IRecipeRepository recipes, TimeProvider time)
{
    public Task<OperationResult<IReadOnlyList<ProductionOrderView>>> ListAsync(CancellationToken cancellationToken = default) =>
        operations.RunAsync<IReadOnlyList<ProductionOrderView>>([ManufacturingRoles.Operator, ManufacturingRoles.Supervisor, ManufacturingRoles.Quality], false, async (_, ct) =>
        {
            var result = new List<ProductionOrderView>();
            foreach (var number in await orders.GetNumbersAsync(ct))
                result.Add(View(ApplicationOperation.Require(await orders.GetByNumberAsync(number, ct))));
            return result;
        }, cancellationToken);

    public Task<OperationResult<ProductionOrderView>> CreateAsync(CreateProductionOrderRequest request, CancellationToken cancellationToken = default) =>
        operations.RunAsync<ProductionOrderView>([ManufacturingRoles.Supervisor], true, async (actor, ct) =>
        {
            ArgumentNullException.ThrowIfNull(request);
            var number = new ProductionOrderNumber(ApplicationOperation.Text(request.OrderNumber, 40));
            var product = new ProductCode(ApplicationOperation.Text(request.ProductCode, 40));
            var name = ApplicationOperation.Text(request.ProductName, 160);
            if (request.Quantity <= 0 || request.Quantity > 999999999999999.999m || decimal.Round(request.Quantity, 3) != request.Quantity)
                throw new ArgumentException("Quantity cannot be represented in the approved schema.");
            var recipe = ApplicationOperation.Require(await recipes.GetByCodeAsync(new RecipeCode(ApplicationOperation.Text(request.RecipeCode, 40)), ct));
            ApplicationOperation.RequireRule(recipe.IsApproved && !await orders.ExistsAsync(number, ct));
            var now = time.GetUtcNow();
            var order = new ProductionOrder(new ProductionOrderId(Guid.NewGuid()), number, product, name, recipe.RecipeId, request.Quantity, now);
            await orders.AddAsync(order, ct);
            await orders.AppendAuditAsync(Audit(order, actor, AuditEventType.ProductionOrderCreated, now), ct);
            return View(order);
        }, cancellationToken);

    public Task<OperationResult<ProductionOrderView>> GetAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        operations.RunAsync<ProductionOrderView>([ManufacturingRoles.Operator, ManufacturingRoles.Supervisor, ManufacturingRoles.Quality], false,
            async (_, ct) => View(ApplicationOperation.Require(await orders.GetByNumberAsync(new ProductionOrderNumber(ApplicationOperation.Text(orderNumber, 40)), ct))), cancellationToken);

    public Task<OperationResult<ProductionOrderView>> ChangeStatusAsync(string orderNumber, ProductionOrderAction action, CancellationToken cancellationToken = default) =>
        operations.RunAsync<ProductionOrderView>([ManufacturingRoles.Supervisor], true, async (actor, ct) =>
        {
            var order = ApplicationOperation.Require(await orders.GetByNumberAsync(new ProductionOrderNumber(ApplicationOperation.Text(orderNumber, 40)), ct));
            switch (action)
            {
                case ProductionOrderAction.Release: order.Release(); break;
                case ProductionOrderAction.StartProcessing: order.StartProcessing(); break;
                case ProductionOrderAction.Complete: order.Complete(); break;
                case ProductionOrderAction.Cancel: order.Cancel(); break;
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
            await orders.UpdateAsync(order, ct);
            await orders.AppendAuditAsync(Audit(order, actor, AuditEventType.ProductionOrderStatusChanged, time.GetUtcNow()), ct);
            return View(order);
        }, cancellationToken);

    public Task<OperationResult<IReadOnlyList<AuditView>>> GetAuditAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        operations.RunAsync<IReadOnlyList<AuditView>>([ManufacturingRoles.Supervisor, ManufacturingRoles.Quality], false, async (_, ct) =>
        {
            var number = new ProductionOrderNumber(ApplicationOperation.Text(orderNumber, 40));
            ApplicationOperation.Require(await orders.GetByNumberAsync(number, ct));
            return (await orders.GetAuditAsync(number, ct)).Select(BatchService.AuditView).ToArray();
        }, cancellationToken);

    private static AuditEvent Audit(ProductionOrder order, AuthenticatedActor actor, AuditEventType action, DateTimeOffset now) =>
        new(new AuditEventId(Guid.NewGuid().ToString("D")), null, actor.ActorId, action, nameof(ProductionOrder), order.ProductionOrderNumber.Value, now, order.ProductionOrderStatus.ToString());
    private static ProductionOrderView View(ProductionOrder order) => new(order.ProductionOrderNumber.Value, order.ProductCode.Value,
        order.ProductName, order.RecipeId.Value, order.Quantity, order.ProductionOrderStatus);
}
