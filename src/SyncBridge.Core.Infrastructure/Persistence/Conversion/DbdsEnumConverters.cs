using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Conversion;

internal static class DbdsEnumConverters
{
    internal static ValueConverter<ProductionOrderStatus, string> ProductionOrderStatus { get; } =
        new(status => ToProductionOrderStatus(status), value => FromProductionOrderStatus(value));

    internal static ValueConverter<BatchStatus, string> BatchStatus { get; } =
        new(status => ToBatchStatus(status), value => FromBatchStatus(value));

    internal static ValueConverter<StepExecutionStatus, string> StepExecutionStatus { get; } =
        new(status => ToStepExecutionStatus(status), value => FromStepExecutionStatus(value));

    internal static ValueConverter<DeviationStatus, string> DeviationStatus { get; } =
        new(status => ToDeviationStatus(status), value => FromDeviationStatus(value));

    internal static ValueConverter<ElectronicSignatureMeaning, string> ElectronicSignatureMeaning { get; } =
        new(meaning => ToElectronicSignatureMeaning(meaning), value => FromElectronicSignatureMeaning(value));

    internal static ValueConverter<AuditEventType, string> AuditEventType { get; } =
        new(action => ToAuditEventType(action), value => FromAuditEventType(value));

    private static string ToProductionOrderStatus(ProductionOrderStatus status) => status switch
    {
        SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Planned => "Planned",
        SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Released => "Released",
        SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.InProcess => "In Process",
        SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Completed => "Completed",
        SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static ProductionOrderStatus FromProductionOrderStatus(string value) => value switch
    {
        "Planned" => SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Planned,
        "Released" => SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Released,
        "In Process" => SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.InProcess,
        "Completed" => SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Completed,
        "Cancelled" => SyncBridge.Core.Domain.Enumerations.ProductionOrderStatus.Cancelled,
        _ => throw new InvalidOperationException($"Unsupported ProductionOrder status '{value}'.")
    };

    private static string ToBatchStatus(BatchStatus status) => status switch
    {
        SyncBridge.Core.Domain.Enumerations.BatchStatus.Created => "Created",
        SyncBridge.Core.Domain.Enumerations.BatchStatus.Ready => "Ready",
        SyncBridge.Core.Domain.Enumerations.BatchStatus.InProgress => "In Progress",
        SyncBridge.Core.Domain.Enumerations.BatchStatus.Exception => "Exception",
        SyncBridge.Core.Domain.Enumerations.BatchStatus.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static BatchStatus FromBatchStatus(string value) => value switch
    {
        "Created" => SyncBridge.Core.Domain.Enumerations.BatchStatus.Created,
        "Ready" => SyncBridge.Core.Domain.Enumerations.BatchStatus.Ready,
        "In Progress" => SyncBridge.Core.Domain.Enumerations.BatchStatus.InProgress,
        "Exception" => SyncBridge.Core.Domain.Enumerations.BatchStatus.Exception,
        "Completed" => SyncBridge.Core.Domain.Enumerations.BatchStatus.Completed,
        _ => throw new InvalidOperationException($"Unsupported Batch status '{value}'.")
    };

    private static string ToStepExecutionStatus(StepExecutionStatus status) => status switch
    {
        SyncBridge.Core.Domain.Enumerations.StepExecutionStatus.Pending => "Pending",
        SyncBridge.Core.Domain.Enumerations.StepExecutionStatus.InProgress => "In Progress",
        SyncBridge.Core.Domain.Enumerations.StepExecutionStatus.Completed => "Completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static StepExecutionStatus FromStepExecutionStatus(string value) => value switch
    {
        "Pending" => SyncBridge.Core.Domain.Enumerations.StepExecutionStatus.Pending,
        "In Progress" => SyncBridge.Core.Domain.Enumerations.StepExecutionStatus.InProgress,
        "Completed" => SyncBridge.Core.Domain.Enumerations.StepExecutionStatus.Completed,
        _ => throw new InvalidOperationException($"Unsupported step-execution status '{value}'.")
    };

    private static string ToDeviationStatus(DeviationStatus status) => status switch
    {
        SyncBridge.Core.Domain.Enumerations.DeviationStatus.Open => "Open",
        SyncBridge.Core.Domain.Enumerations.DeviationStatus.UnderReview => "Under Review",
        SyncBridge.Core.Domain.Enumerations.DeviationStatus.Approved => "Approved",
        SyncBridge.Core.Domain.Enumerations.DeviationStatus.Rejected => "Rejected",
        SyncBridge.Core.Domain.Enumerations.DeviationStatus.Closed => "Closed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static DeviationStatus FromDeviationStatus(string value) => value switch
    {
        "Open" => SyncBridge.Core.Domain.Enumerations.DeviationStatus.Open,
        "Under Review" => SyncBridge.Core.Domain.Enumerations.DeviationStatus.UnderReview,
        "Approved" => SyncBridge.Core.Domain.Enumerations.DeviationStatus.Approved,
        "Rejected" => SyncBridge.Core.Domain.Enumerations.DeviationStatus.Rejected,
        "Closed" => SyncBridge.Core.Domain.Enumerations.DeviationStatus.Closed,
        _ => throw new InvalidOperationException($"Unsupported deviation status '{value}'.")
    };

    private static string ToElectronicSignatureMeaning(ElectronicSignatureMeaning meaning) =>
        Enum.IsDefined(meaning)
            ? meaning.ToString()
            : throw new ArgumentOutOfRangeException(nameof(meaning));

    private static ElectronicSignatureMeaning FromElectronicSignatureMeaning(string value) =>
        Enum.TryParse<ElectronicSignatureMeaning>(value, false, out var meaning) && Enum.IsDefined(meaning)
            ? meaning
            : throw new InvalidOperationException($"Unsupported electronic-signature meaning '{value}'.");

    private static string ToAuditEventType(AuditEventType action) =>
        Enum.IsDefined(action)
            ? action.ToString()
            : throw new ArgumentOutOfRangeException(nameof(action));

    private static AuditEventType FromAuditEventType(string value) =>
        Enum.TryParse<AuditEventType>(value, false, out var action) && Enum.IsDefined(action)
            ? action
            : throw new InvalidOperationException($"Unsupported audit-event action '{value}'.");
}
