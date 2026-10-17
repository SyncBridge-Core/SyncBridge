using Microsoft.AspNetCore.Mvc;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Pages;

public sealed class ShopFloorModel : ManufacturingPageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await LoadActorAsync();
        if (Actor is null) return Failure(OperationOutcome.TechnicalFailure);
        return IsOperator ? Page() : Failure(OperationOutcome.Forbidden);
    }
}
