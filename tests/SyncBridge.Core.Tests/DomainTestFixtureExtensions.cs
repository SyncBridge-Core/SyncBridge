using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

internal static class DomainTestFixtureExtensions
{
    private static readonly OperatorId TestOperator = new("OP-TEST");
    private static readonly DateTimeOffset TestTimestamp =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    internal static ProductionOrder CreateProductionOrder(
        string orderNumber = "PO-001",
        decimal quantity = 100m) =>
        new(
            new ProductionOrderId(Guid.Parse("22222222-2222-2222-2222-222222222222")),
            new ProductionOrderNumber(orderNumber),
            new ProductCode("PRODUCT-001"),
            "Test product",
            new RecipeId("11111111-1111-1111-1111-111111111111"),
            quantity,
            TestTimestamp);

    internal static void MarkReady(this Batch batch) =>
        batch.MarkReady(TestOperator, TestTimestamp);

    internal static void StartProcessing(
        this Batch batch,
        Recipe recipe,
        IEnumerable<MaterialVerification> materialVerifications,
        IEnumerable<EquipmentVerification> equipmentVerifications) =>
        batch.StartProcessing(
            recipe,
            materialVerifications,
            equipmentVerifications,
            TestOperator,
            TestTimestamp);

    internal static void EnterException(this Batch batch, params Deviation[] deviations) =>
        batch.EnterException(TestOperator, TestTimestamp, deviations);

    internal static void ResumeProcessing(this Batch batch) =>
        batch.ResumeProcessing(TestOperator, TestTimestamp);

    internal static void Complete(this Batch batch) =>
        batch.Complete(
            new Recipe(
                new RecipeId("LEGACY-FIXTURE-RECIPE"),
                batch.RecipeCode,
                "Legacy test fixture recipe",
                "1.0",
                true),
            TestOperator,
            TestTimestamp);

    internal static void StartStep(
        this Batch batch,
        Recipe recipe,
        BatchStepExecution execution,
        DateTimeOffset startedAt) =>
        batch.StartStep(recipe, execution, startedAt, TestOperator);

    internal static void BeginReview(this Deviation deviation, string reviewComments) =>
        deviation.BeginReview(reviewComments, TestOperator, TestTimestamp);

    internal static void Approve(this Deviation deviation) =>
        deviation.Approve(TestOperator, TestTimestamp);

    internal static void Reject(this Deviation deviation) =>
        deviation.Reject(TestOperator, TestTimestamp);

    internal static void Resolve(
        this Deviation deviation,
        string finalDisposition,
        DateTimeOffset resolvedAt) =>
        deviation.Resolve(finalDisposition, resolvedAt, TestOperator);
}
