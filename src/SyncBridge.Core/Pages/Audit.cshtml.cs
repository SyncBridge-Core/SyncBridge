using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages;

public sealed class AuditModel(ProductionOrderService orders, BatchService batches) : ManufacturingPageModel
{
    public IReadOnlyList<AuditView> Events { get; private set; } = [];
    public string Record { get; private set; } = "";
    public async Task<IActionResult> OnGetAsync(string? orderNumber, string? batchNumber)
    {
        await LoadActorAsync();
        if ((orderNumber is null) == (batchNumber is null)) return Failure(OperationOutcome.InvalidInput);
        var result = batchNumber is not null ? await batches.GetAuditAsync(batchNumber, HttpContext.RequestAborted) : await orders.GetAuditAsync(orderNumber!, HttpContext.RequestAborted);
        if (!result.Succeeded) return Failure(result.Outcome);
        Events = result.Value!;
        Record = batchNumber ?? orderNumber!;
        return Page();
    }
}
