using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages.Orders;

public sealed class DetailsModel(ProductionOrderService orders) : ManufacturingPageModel
{
    public ProductionOrderView Order { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(string number)
    {
        await LoadActorAsync();
        var result = await orders.GetAsync(number, HttpContext.RequestAborted);
        if (!result.Succeeded) return Failure(result.Outcome);
        Order = result.Value!;
        return Page();
    }
    public Task<IActionResult> OnPostStatusAsync(string number, [BindRequired] ProductionOrderAction action) =>
        ModelState.IsValid ? CommandAsync(orders.ChangeStatusAsync(number, action, HttpContext.RequestAborted), new { number }, () => OnGetAsync(number))
        : Task.FromResult(Failure(OperationOutcome.InvalidInput));
}
