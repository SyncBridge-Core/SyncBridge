using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class VerificationEvidenceTests
{
    [Fact]
    public void MaterialVerification_PreservesApprovedEvidenceFields()
    {
        var batchNumber = new BatchNumber("BATCH-001");
        var materialCode = new MaterialCode("MAT-001");
        var operatorId = new OperatorId("OP-001");
        var verifiedAt = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        var verification = new MaterialVerification(
            batchNumber,
            materialCode,
            operatorId,
            verifiedAt,
            VerificationResult.Passed);

        Assert.Equal(batchNumber, verification.BatchNumber);
        Assert.Equal(materialCode, verification.MaterialCode);
        Assert.Equal(operatorId, verification.OperatorId);
        Assert.Equal(verifiedAt, verification.VerifiedAt);
        Assert.Equal(VerificationResult.Passed, verification.VerificationResult);
    }

    [Fact]
    public void MaterialVerification_WithNullBatchNumber_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MaterialVerification(
                null!,
                new MaterialCode("MAT-001"),
                new OperatorId("OP-001"),
                DateTimeOffset.UtcNow,
                VerificationResult.Passed));
    }

    [Fact]
    public void MaterialVerification_WithNullMaterialCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MaterialVerification(
                new BatchNumber("BATCH-001"),
                null!,
                new OperatorId("OP-001"),
                DateTimeOffset.UtcNow,
                VerificationResult.Passed));
    }

    [Fact]
    public void MaterialVerification_WithNullOperatorId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new MaterialVerification(
                new BatchNumber("BATCH-001"),
                new MaterialCode("MAT-001"),
                null!,
                DateTimeOffset.UtcNow,
                VerificationResult.Passed));
    }

    [Fact]
    public void EquipmentVerification_PreservesApprovedEvidenceFields()
    {
        var batchNumber = new BatchNumber("BATCH-001");
        var equipmentCode = new EquipmentCode("EQ-001");
        var operatorId = new OperatorId("OP-001");
        var verifiedAt = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

        var verification = new EquipmentVerification(
            batchNumber,
            equipmentCode,
            operatorId,
            verifiedAt,
            VerificationResult.Passed);

        Assert.Equal(batchNumber, verification.BatchNumber);
        Assert.Equal(equipmentCode, verification.EquipmentCode);
        Assert.Equal(operatorId, verification.OperatorId);
        Assert.Equal(verifiedAt, verification.VerifiedAt);
        Assert.Equal(VerificationResult.Passed, verification.VerificationResult);
    }

    [Fact]
    public void EquipmentVerification_WithNullBatchNumber_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EquipmentVerification(
                null!,
                new EquipmentCode("EQ-001"),
                new OperatorId("OP-001"),
                DateTimeOffset.UtcNow,
                VerificationResult.Passed));
    }

    [Fact]
    public void EquipmentVerification_WithNullEquipmentCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EquipmentVerification(
                new BatchNumber("BATCH-001"),
                null!,
                new OperatorId("OP-001"),
                DateTimeOffset.UtcNow,
                VerificationResult.Passed));
    }

    [Fact]
    public void EquipmentVerification_WithNullOperatorId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EquipmentVerification(
                new BatchNumber("BATCH-001"),
                new EquipmentCode("EQ-001"),
                null!,
                DateTimeOffset.UtcNow,
                VerificationResult.Passed));
    }
}
