using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SyncBridge.Core.Application.Contracts.Identity;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages;

[Authorize]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public abstract class ManufacturingPageModel : PageModel
{
    public AuthenticatedActor? Actor { get; private set; }
    public bool IsOperator => Actor?.Roles.Contains(ManufacturingRoles.Operator) == true;
    public bool IsSupervisor => Actor?.Roles.Contains(ManufacturingRoles.Supervisor) == true;
    public bool IsQuality => Actor?.Roles.Contains(ManufacturingRoles.Quality) == true;
    public bool CanReview => IsSupervisor || IsQuality;
    [TempData] public string? Feedback { get; set; }
    private bool actorLoadFailed;
    protected async Task LoadActorAsync()
    {
        try { Actor = await HttpContext.RequestServices.GetRequiredService<IActorContext>().GetActorAsync(HttpContext.RequestAborted); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            actorLoadFailed = true;
            HttpContext.RequestServices.GetRequiredService<ILogger<ManufacturingPageModel>>().LogError(exception, "Manufacturing navigation identity could not be resolved.");
        }
    }
    protected IActionResult Failure(OperationOutcome outcome)
    {
        if (actorLoadFailed) outcome = OperationOutcome.TechnicalFailure;
        var (status, message) = outcome switch
        {
            OperationOutcome.Unauthenticated => (401, "Please sign in again."),
            OperationOutcome.Forbidden => (403, "Your role does not permit this operation."),
            OperationOutcome.NotFound => (404, "The requested manufacturing record was not found."),
            OperationOutcome.TechnicalFailure => (503, "The operation could not be completed. Try again later."),
            _ => (422, "The operation was rejected. Check the input, current state, prerequisites and required approvals.")
        };
        return new ContentResult { StatusCode = status, Content = message, ContentType = "text/plain" };
    }
    protected async Task<IActionResult> CommandAsync<T>(Task<OperationResult<T>> command, object routeValues, Func<Task<IActionResult>> reload)
    {
        var result = await command;
        if (result.Succeeded) { Feedback = "Operation completed successfully."; return RedirectToPage(routeValues); }
        if (result.Outcome is not (OperationOutcome.InvalidInput or OperationOutcome.BusinessRuleRejected)) return Failure(result.Outcome);
        ModelState.AddModelError(string.Empty, "The operation was rejected. Check the input, current state, prerequisites and required approvals.");
        Response.StatusCode = 422;
        return await reload();
    }
}
