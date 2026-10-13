using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.Exceptions;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class SprintClosureModelTests
{
    [Fact]
    public void ProductionOrder_Constructor_PreservesDbdsAlignedFields()
    {
        var productionOrderId = new ProductionOrderId(Guid.NewGuid());
        var orderNumber = new ProductionOrderNumber("PO-001");
        var productCode = new ProductCode("PRODUCT-001");
        var recipeId = new RecipeId("RECIPE-ID-001");
        var createdAt = Timestamp(1);

        var order = new ProductionOrder(
            productionOrderId,
            orderNumber,
            productCode,
            "Tablet Product",
            recipeId,
            125.500m,
            createdAt);

        Assert.Equal(productionOrderId, order.ProductionOrderId);
        Assert.Equal(orderNumber, order.ProductionOrderNumber);
        Assert.Equal(productCode, order.ProductCode);
        Assert.Equal("Tablet Product", order.ProductName);
        Assert.Equal(recipeId, order.RecipeId);
        Assert.Equal(125.500m, order.Quantity);
        Assert.Equal(ProductionOrderStatus.Planned, order.ProductionOrderStatus);
        Assert.Equal(createdAt, order.CreatedAt);
    }

    [Fact]
    public void ProductionOrder_WithBlankProductName_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => CreateProductionOrder(productName: " "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ProductionOrder_WithNonPositiveQuantity_IsRejected(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateProductionOrder(quantity: quantity));
    }

    [Fact]
    public void ProductionOrder_ExistingLifecycle_RemainsEnforced()
    {
        var order = CreateProductionOrder();

        order.Release();
        order.StartProcessing();
        order.Complete();

        Assert.Equal(ProductionOrderStatus.Completed, order.ProductionOrderStatus);
        Assert.Throws<InvalidProductionOrderStateTransitionException>(order.Cancel);
    }

    [Fact]
    public void Material_Constructor_PreservesReferenceIdentityCodeAndName()
    {
        var id = new MaterialId(Guid.NewGuid());
        var code = new MaterialCode("MAT-001");

        var material = new Material(id, code, "Active ingredient");

        Assert.Equal(id, material.MaterialId);
        Assert.Equal(code, material.MaterialCode);
        Assert.Equal("Active ingredient", material.Name);
    }

    [Fact]
    public void Material_WithBlankName_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new Material(
            new MaterialId(Guid.NewGuid()),
            new MaterialCode("MAT-001"),
            " "));
    }

    [Fact]
    public void Equipment_Constructor_PreservesOptionalCalibrationDate()
    {
        var id = new EquipmentId(Guid.NewGuid());
        var code = new EquipmentCode("EQ-001");
        var calibrationDueDate = new DateOnly(2027, 1, 31);

        var equipment = new Equipment(id, code, "Tablet press", calibrationDueDate);

        Assert.Equal(id, equipment.EquipmentId);
        Assert.Equal(code, equipment.EquipmentCode);
        Assert.Equal("Tablet press", equipment.Name);
        Assert.Equal(calibrationDueDate, equipment.CalibrationDueDate);
    }

    [Fact]
    public void Equipment_Constructor_AllowsNoCalibrationDate()
    {
        var equipment = new Equipment(
            new EquipmentId(Guid.NewGuid()),
            new EquipmentCode("EQ-001"),
            "Tablet press");

        Assert.Null(equipment.CalibrationDueDate);
    }

    private static ProductionOrder CreateProductionOrder(
        string productName = "Tablet Product",
        decimal quantity = 100m)
    {
        return new ProductionOrder(
            new ProductionOrderId(Guid.NewGuid()),
            new ProductionOrderNumber("PO-001"),
            new ProductCode("PRODUCT-001"),
            productName,
            new RecipeId("RECIPE-ID-001"),
            quantity,
            Timestamp(1));
    }

    private static DateTimeOffset Timestamp(int hour) =>
        new(2026, 10, 6, hour, 0, 0, TimeSpan.Zero);
}
