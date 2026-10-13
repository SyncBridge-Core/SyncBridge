using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.ValueObjects;
using SyncBridge.Core.Infrastructure.Persistence.Conversion;
using SyncBridge.Core.Infrastructure.Persistence.Validation;

namespace SyncBridge.Core.Tests;

public sealed class PersistenceBoundaryTests
{
    [Fact]
    public void BatchCreation_DerivesFullQuantityAndOrderNumberFromProductionOrder()
    {
        var productionOrder = DomainTestFixtureExtensions.CreateProductionOrder("PO-FULL", 275.125m);

        var batch = new Batch(
            new BatchNumber("BATCH-FULL"),
            productionOrder,
            new RecipeCode("RECIPE-001"),
            new OperatorId("OP-TEST"),
            new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(productionOrder.ProductionOrderNumber, batch.ProductionOrderNumber);
        Assert.Equal(productionOrder.Quantity, batch.PlannedQuantity);
    }

    [Fact]
    public void MultipleBatches_ForOneProductionOrder_EachReceiveTheFullQuantity()
    {
        var productionOrder = DomainTestFixtureExtensions.CreateProductionOrder(quantity: 80m);

        var first = CreateBatch("BATCH-001", productionOrder);
        var second = CreateBatch("BATCH-002", productionOrder);

        Assert.Equal(80m, first.PlannedQuantity);
        Assert.Equal(80m, second.PlannedQuantity);
    }

    [Fact]
    public void TechnicalGuidConverter_RoundTripsExactLowercaseCanonicalText()
    {
        const string identifier = "12345678-1234-1234-1234-123456789abc";

        var parsed = TechnicalGuidConverter.ToGuid(identifier);

        Assert.Equal(identifier, TechnicalGuidConverter.ToCanonicalString(parsed));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678123412341234123456789abc")]
    [InlineData("12345678-1234-1234-1234-123456789ABC")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    public void TechnicalGuidConverter_RejectsInvalidOrNoncanonicalText(string value)
    {
        Assert.Throws<ArgumentException>(() => TechnicalGuidConverter.ToGuid(value));
    }

    [Fact]
    public void DbdsLengthValidator_AcceptsMaximumVerificationAuditIdentityWithoutChangingIt()
    {
        var value = new string('x', 180);

        var validated = DbdsStringLengthValidator.RequireWithinLimit(value, 256, "affectedEntityId");

        Assert.Same(value, validated);
    }

    [Fact]
    public void DbdsLengthValidator_CountsUtf16CodeUnitsAndRejectsOverflowWithoutTruncation()
    {
        var exactlyAtLimit = string.Concat(Enumerable.Repeat("😀", 128));
        var overLimit = string.Concat(Enumerable.Repeat("😀", 129));

        Assert.Equal(256, exactlyAtLimit.Length);
        Assert.Same(
            exactlyAtLimit,
            DbdsStringLengthValidator.RequireWithinLimit(exactlyAtLimit, 256, "value"));
        Assert.Throws<ArgumentException>(
            () => DbdsStringLengthValidator.RequireWithinLimit(overLimit, 256, "value"));
    }

    private static Batch CreateBatch(string batchNumber, ProductionOrder productionOrder) =>
        new(
            new BatchNumber(batchNumber),
            productionOrder,
            new RecipeCode("RECIPE-001"),
            new OperatorId("OP-TEST"),
            new DateTimeOffset(2026, 10, 12, 12, 0, 0, TimeSpan.Zero));
}
