using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages;

public sealed class IndexModel(ProductionOrderService orders, BatchService batches) : ManufacturingPageModel
{
    public IReadOnlyList<ProductionOrderView> Orders { get; private set; } = [];
    public IReadOnlyList<BatchSummaryView> Batches { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadActorAsync();
        if (Actor is not null && !(IsOperator || IsSupervisor || IsQuality)) return Page();
        var orderResult = await orders.ListAsync(HttpContext.RequestAborted);
        if (!orderResult.Succeeded) return Failure(orderResult.Outcome);
        var batchResult = await batches.ListAsync(HttpContext.RequestAborted);
        if (!batchResult.Succeeded) return Failure(batchResult.Outcome);
        Orders = orderResult.Value!;
        Batches = batchResult.Value!;
        return Page();
    }
}
