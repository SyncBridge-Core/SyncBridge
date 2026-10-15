using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Application.Services;

public enum OperationOutcome { Success, InvalidInput, BusinessRuleRejected, Unauthenticated, Forbidden, NotFound, TechnicalFailure }

public sealed record OperationResult<T>(OperationOutcome Outcome, T? Value = default, string? Message = null)
{
    public bool Succeeded => Outcome == OperationOutcome.Success;
}

public sealed record CreateProductionOrderRequest(string OrderNumber, string ProductCode, string ProductName, string RecipeCode, decimal Quantity);
public enum ProductionOrderAction { Release, StartProcessing, Complete, Cancel }
public enum DeviationAction { BeginReview, Approve, Reject, Resolve }
public sealed record ProductionOrderView(string OrderNumber, string ProductCode, string ProductName, string RecipeId, decimal Quantity, ProductionOrderStatus Status);
public sealed record BatchSummaryView(string BatchNumber, string OrderNumber, BatchStatus Status);
public sealed record StepView(string ExecutionId, int StepNumber, StepExecutionStatus Status, bool RequiresSignature, string? RecordedData);
public sealed record RecipeStepView(string StepId, int StepNumber, string Name, string Instructions, bool RequiresSignature);
public sealed record MaterialRequirementView(string MaterialCode, decimal? RequiredQuantity);
public sealed record RecipeView(string Code, string Name, string Version, IReadOnlyList<RecipeStepView> Steps, IReadOnlyList<MaterialRequirementView> Materials, IReadOnlyList<string> Equipment);
public sealed record DeviationView(string Code, string? ExecutionId, string Description, DeviationStatus Status, string? ReviewComments, string? FinalDisposition);
public sealed record VerificationView(string ItemCode, VerificationResult Result, string ActorId, DateTimeOffset VerifiedAt);
public sealed record SignatureView(string SignatureId, string ExecutionId, string ActorId, ElectronicSignatureMeaning Meaning, DateTimeOffset SignedAt);
public sealed record BatchView(string BatchNumber, string OrderNumber, string RecipeCode, decimal PlannedQuantity, BatchStatus Status, RecipeView Recipe,
    IReadOnlyList<StepView> Steps, IReadOnlyList<DeviationView> Deviations,
    IReadOnlyList<VerificationView> Materials, IReadOnlyList<VerificationView> Equipment, IReadOnlyList<SignatureView> Signatures);
public sealed record AuditView(string Id, string ActorId, AuditEventType Action, string Entity, string EntityId, DateTimeOffset OccurredAt, string? ResultingState, string? Details);
public sealed record ExceptionReviewView(string BatchNumber, IReadOnlyList<string> UnresolvedDeviations, IReadOnlyList<string> MissingOrFailedMaterials, IReadOnlyList<string> MissingOrFailedEquipment);

internal sealed class OperationRejection(OperationOutcome outcome) : Exception
{
    public OperationOutcome Outcome { get; } = outcome;
}
