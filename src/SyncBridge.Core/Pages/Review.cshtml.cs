using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages;

public sealed class ReviewModel(BatchService batches) : ManufacturingPageModel
{
    public IReadOnlyList<ExceptionReviewView> Exceptions { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadActorAsync();
        var result = await batches.ReviewExceptionsAsync(HttpContext.RequestAborted);
        if (!result.Succeeded) return Failure(result.Outcome);
        Exceptions = result.Value!;
        return Page();
    }
}
