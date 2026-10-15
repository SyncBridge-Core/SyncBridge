using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SyncBridge.Core.Application.Services;
using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Pages.Batches;

public sealed class DetailsModel(BatchService batches) : ManufacturingPageModel
{
    public BatchView Batch { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(string number)
    {
        await LoadActorAsync();
        var result = await batches.GetAsync(number, HttpContext.RequestAborted);
        if (!result.Succeeded) return Failure(result.Outcome);
        Batch = result.Value!;
        return Page();
    }
    private Task<IActionResult> Run(string number, Func<Task<OperationResult<BatchView>>> command) =>
        ModelState.IsValid ? CommandAsync(command(), new { number }, () => OnGetAsync(number)) : Task.FromResult(Failure(OperationOutcome.InvalidInput));
    public Task<IActionResult> OnPostReadyAsync(string number) => Run(number, () => batches.MarkReadyAsync(number, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostStartAsync(string number) => Run(number, () => batches.StartProcessingAsync(number, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostMaterialAsync(string number, string itemCode, [BindRequired] VerificationResult result) => Run(number, () => batches.VerifyMaterialAsync(number, itemCode, result, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostEquipmentAsync(string number, string itemCode, [BindRequired] VerificationResult result) => Run(number, () => batches.VerifyEquipmentAsync(number, itemCode, result, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostStartStepAsync(string number, [BindRequired] int stepNumber) => Run(number, () => batches.StartStepAsync(number, stepNumber, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostCompleteStepAsync(string number, string executionId, string? signatureId, string? recordedData) =>
        Run(number, () => batches.CompleteStepAsync(number, executionId, signatureId, recordedData, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostSignAsync(string number, string executionId, [BindRequired] ElectronicSignatureMeaning meaning) =>
        Run(number, () => batches.SignStepAsync(number, executionId, meaning, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostDeviationAsync(string number, string deviationCode, string description, string? executionId) =>
        Run(number, () => batches.CreateDeviationAsync(number, deviationCode, description, executionId, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostReviewAsync(string number, string deviationCode, [BindRequired] DeviationAction action, string? commentsOrDisposition) =>
        Run(number, () => batches.ReviewDeviationAsync(number, deviationCode, action, commentsOrDisposition, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostResumeAsync(string number) => Run(number, () => batches.ResumeAsync(number, HttpContext.RequestAborted));
    public Task<IActionResult> OnPostCompleteAsync(string number) => Run(number, () => batches.CompleteAsync(number, HttpContext.RequestAborted));
}
