using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages.Orders;

public sealed class CreateModel(ProductionOrderService orders) : ManufacturingPageModel
{
    [BindProperty, Required, StringLength(40)] public string OrderNumber { get; set; } = "";
    [BindProperty, Required, StringLength(40)] public string ProductCode { get; set; } = "";
    [BindProperty, Required, StringLength(160)] public string ProductName { get; set; } = "";
    [BindProperty, Required, StringLength(40)] public string RecipeCode { get; set; } = "";
    [BindProperty] public decimal Quantity { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadActorAsync();
        return IsSupervisor ? Page() : Failure(OperationOutcome.Forbidden);
    }
    public async Task<IActionResult> OnPostAsync()
    {
        await LoadActorAsync();
        if (!IsSupervisor) return Failure(OperationOutcome.Forbidden);
        if (!ModelState.IsValid) { Response.StatusCode = 422; return Page(); }
        var result = await orders.CreateAsync(new(OrderNumber, ProductCode, ProductName, RecipeCode, Quantity), HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            if (result.Outcome is not (OperationOutcome.InvalidInput or OperationOutcome.BusinessRuleRejected)) return Failure(result.Outcome);
            ModelState.AddModelError("", "Order creation was rejected. Check the quantity, unique order number and approved recipe code.");
            Response.StatusCode = 422;
            return Page();
        }
        Feedback = "Production order created.";
        return RedirectToPage("Details", new { number = result.Value!.OrderNumber });
    }
}
