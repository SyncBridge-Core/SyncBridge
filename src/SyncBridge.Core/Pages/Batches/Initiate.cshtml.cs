using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages.Batches;

public sealed class InitiateModel(ProductionOrderService orders, BatchService batches) : ManufacturingPageModel
{
    [BindProperty(SupportsGet = true), Required, StringLength(40)] public string OrderNumber { get; set; } = "";
    [BindProperty, Required, StringLength(40)] public string BatchNumber { get; set; } = "";
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadActorAsync();
        if (!IsOperator) return Failure(OperationOutcome.Forbidden);
        var result = await orders.GetAsync(OrderNumber, HttpContext.RequestAborted);
        return result.Succeeded ? Page() : Failure(result.Outcome);
    }
    public async Task<IActionResult> OnPostAsync()
    {
        await LoadActorAsync();
        if (!IsOperator) return Failure(OperationOutcome.Forbidden);
        if (!ModelState.IsValid) { Response.StatusCode = 422; return Page(); }
        var result = await batches.InitiateAsync(OrderNumber, BatchNumber, HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            if (result.Outcome is not (OperationOutcome.InvalidInput or OperationOutcome.BusinessRuleRejected)) return Failure(result.Outcome);
            ModelState.AddModelError("", "Batch initiation was rejected. Select a released or in-process order with an approved recipe and a unique batch number.");
            Response.StatusCode = 422;
            return Page();
        }
        Feedback = "Batch initiated.";
        return RedirectToPage("Details", new { number = result.Value!.BatchNumber });
    }
}
