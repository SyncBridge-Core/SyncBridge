using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class ElectronicSignatureTests
{
    [Fact]
    public void Constructor_PreservesControlledRecordEvidence()
    {
        var signatureId = new ElectronicSignatureId("SIG-001");
        var batchNumber = new BatchNumber("BATCH-001");
        var recordType = RelatedRecordType.BatchStepExecution;
        var recordId = new RelatedRecordId("EXEC-001");
        var operatorId = new OperatorId("OP-001");
        var signedAt = Timestamp(1);

        var signature = new ElectronicSignature(
            signatureId,
            batchNumber,
            recordType,
            recordId,
            operatorId,
            ElectronicSignatureMeaning.Completion,
            signedAt);

        Assert.Equal(signatureId, signature.ElectronicSignatureId);
        Assert.Equal(batchNumber, signature.BatchNumber);
        Assert.Equal(recordType, signature.RelatedRecordType);
        Assert.Equal(recordId, signature.RelatedRecordId);
        Assert.Equal(operatorId, signature.OperatorId);
        Assert.Equal(ElectronicSignatureMeaning.Completion, signature.Meaning);
        Assert.Equal(signedAt, signature.SignedAt);
    }

    [Fact]
    public void Constructor_WithNullSignatureId_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => CreateSignature(null!, ValidBatch(), ValidType(), ValidRecord(), ValidOperator()));

    [Fact]
    public void Constructor_WithNullBatchNumber_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => CreateSignature(ValidId(), null!, ValidType(), ValidRecord(), ValidOperator()));

    [Fact]
    public void Constructor_WithNullRelatedRecordType_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => CreateSignature(ValidId(), ValidBatch(), null!, ValidRecord(), ValidOperator()));

    [Fact]
    public void Constructor_WithNullRelatedRecordId_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => CreateSignature(ValidId(), ValidBatch(), ValidType(), null!, ValidOperator()));

    [Fact]
    public void Constructor_WithNullOperatorId_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => CreateSignature(ValidId(), ValidBatch(), ValidType(), ValidRecord(), null!));

    [Fact]
    public void Constructor_WithUndefinedMeaning_ThrowsArgumentOutOfRangeException() =>
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateSignature(ValidId(), ValidBatch(), ValidType(), ValidRecord(), ValidOperator(), (ElectronicSignatureMeaning)999));

    [Fact]
    public void RelatedRecordType_WithBlankValue_ThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() => new RelatedRecordType(" "));

    private static ElectronicSignature CreateSignature(
        ElectronicSignatureId electronicSignatureId,
        BatchNumber batchNumber,
        RelatedRecordType relatedRecordType,
        RelatedRecordId relatedRecordId,
        OperatorId operatorId,
        ElectronicSignatureMeaning meaning = ElectronicSignatureMeaning.Completion)
    {
        return new ElectronicSignature(
            electronicSignatureId,
            batchNumber,
            relatedRecordType,
            relatedRecordId,
            operatorId,
            meaning,
            Timestamp(1));
    }

    private static ElectronicSignatureId ValidId() => new("SIG-001");

    private static BatchNumber ValidBatch() => new("BATCH-001");

    private static RelatedRecordType ValidType() => RelatedRecordType.BatchStepExecution;

    private static RelatedRecordId ValidRecord() => new("EXEC-001");

    private static OperatorId ValidOperator() => new("OP-001");

    private static DateTimeOffset Timestamp(int hourOffset) =>
        new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero).AddHours(hourOffset);
}
