using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SyncBridge.Core.Application.Services;

namespace SyncBridge.Core.Controllers;

[ApiController]
[Authorize]
[Route("api")]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ManufacturingController(ProductionOrderService orders, BatchService batches) : ControllerBase
{
    [HttpGet("production-orders/{orderNumber}")]
    public async Task<IActionResult> GetOrder(string orderNumber, CancellationToken ct) =>
        Map(await orders.GetAsync(orderNumber, ct));

    [HttpPost("batches")]
    public async Task<IActionResult> Initiate(InitiateBatchRequest request, CancellationToken ct) =>
        Map(await batches.InitiateAsync(request.OrderNumber, request.BatchNumber, ct), StatusCodes.Status201Created);

    [HttpPost("batches/{batchNumber}/steps/{stepNumber:int}/start")]
    public async Task<IActionResult> StartStep(string batchNumber, int stepNumber, CancellationToken ct) =>
        Map(await batches.StartStepAsync(batchNumber, stepNumber, ct));

    [HttpPost("batches/{batchNumber}/steps/{executionId}/complete")]
    public async Task<IActionResult> CompleteStep(string batchNumber, string executionId, CompleteStepRequest request, CancellationToken ct) =>
        Map(await batches.CompleteStepAsync(batchNumber, executionId, request.SignatureId, request.RecordedData, ct));

    [HttpPost("batches/{batchNumber}/deviations")]
    public async Task<IActionResult> Deviation(string batchNumber, CreateDeviationRequest request, CancellationToken ct) =>
        Map(await batches.CreateDeviationAsync(batchNumber, request.DeviationCode, request.Description, request.ExecutionId, ct));

    private IActionResult Map<T>(OperationResult<T> result, int success = StatusCodes.Status200OK)
    {
        if (result.Succeeded) return StatusCode(success, result.Value);
        var status = result.Outcome switch
        {
            OperationOutcome.InvalidInput => 400,
            OperationOutcome.Unauthenticated => 401,
            OperationOutcome.Forbidden => 403,
            OperationOutcome.NotFound => 404,
            OperationOutcome.BusinessRuleRejected => 422,
            _ => 503
        };
        return Problem(statusCode: status, title: result.Outcome.ToString(), detail: result.Message);
    }
}

public sealed record InitiateBatchRequest([Required, StringLength(40)] string OrderNumber, [Required, StringLength(40)] string BatchNumber);
public sealed record CompleteStepRequest(string? SignatureId = null, [StringLength(2000)] string? RecordedData = null);
public sealed record CreateDeviationRequest([Required, StringLength(40)] string DeviationCode,
    [Required, StringLength(2000)] string Description, string? ExecutionId = null);

// Antiforgery failures are authorization-filter results, before controller actions execute.
public sealed class ApiAntiforgeryResultFilter : IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.HttpContext.Request.Path.StartsWithSegments("/api") && context.Result is AntiforgeryValidationFailedResult)
            context.Result = new BadRequestObjectResult(new ProblemDetails { Status = 400, Title = "Invalid antiforgery token." });
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
