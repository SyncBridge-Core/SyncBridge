using SyncBridge.Core.Domain.Entities;
using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void Constructor_PreservesApprovedAuditShape()
    {
        var id = new AuditEventId("AUD-001");
        var batchNumber = new BatchNumber("BATCH-001");
        var actor = new OperatorId("OP-001");
        var occurredAt = Timestamp(1);

        var auditEvent = new AuditEvent(
            id,
            batchNumber,
            actor,
            AuditEventType.BatchCompleted,
            "Batch",
            batchNumber.Value,
            occurredAt,
            BatchStatus.Completed.ToString(),
            "All required steps completed.");

        Assert.Equal(id, auditEvent.AuditEventId);
        Assert.Equal(batchNumber, auditEvent.BatchNumber);
        Assert.Equal(actor, auditEvent.OperatorId);
        Assert.Equal(AuditEventType.BatchCompleted, auditEvent.Action);
        Assert.Equal("Batch", auditEvent.AffectedEntity);
        Assert.Equal("BATCH-001", auditEvent.AffectedEntityId);
        Assert.Equal(occurredAt, auditEvent.OccurredAt);
        Assert.Equal("Completed", auditEvent.ResultingState);
        Assert.Equal("All required steps completed.", auditEvent.Details);
    }

    [Fact]
    public void Constructor_AllowsNoBatchContext()
    {
        var auditEvent = new AuditEvent(
            new AuditEventId("AUD-001"),
            null,
            new OperatorId("OP-001"),
            AuditEventType.BatchCreated,
            "ProductionOrder",
            "PO-001",
            Timestamp(1));

        Assert.Null(auditEvent.BatchNumber);
    }

    [Fact]
    public void Constructor_WhenRequiredInputMissing_RejectsEvidence()
    {
        var id = new AuditEventId("AUD-001");
        var actor = new OperatorId("OP-001");

        Assert.Throws<ArgumentException>(() => new AuditEvent(
            new AuditEventId(" "), null, actor, AuditEventType.BatchCreated,
            "Batch", "BATCH-001", Timestamp(1)));
        Assert.Throws<ArgumentNullException>(() => new AuditEvent(
            id, null, null!, AuditEventType.BatchCreated,
            "Batch", "BATCH-001", Timestamp(1)));
        Assert.Throws<ArgumentException>(() => new AuditEvent(
            id, null, actor, AuditEventType.BatchCreated,
            " ", "BATCH-001", Timestamp(1)));
        Assert.Throws<ArgumentException>(() => new AuditEvent(
            id, null, actor, AuditEventType.BatchCreated,
            "Batch", " ", Timestamp(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuditEvent(
            id, null, actor, (AuditEventType)int.MaxValue,
            "Batch", "BATCH-001", Timestamp(1)));
    }

    [Fact]
    public void Evidence_RemainsUnchangedThroughPublicReadApi()
    {
        var auditEvent = new AuditEvent(
            new AuditEventId("AUD-001"),
            new BatchNumber("BATCH-001"),
            new OperatorId("OP-001"),
            AuditEventType.BatchMarkedReady,
            "Batch",
            "BATCH-001",
            Timestamp(1),
            BatchStatus.Ready.ToString());

        var observed = new
        {
            auditEvent.AuditEventId,
            auditEvent.BatchNumber,
            auditEvent.OperatorId,
            auditEvent.Action,
            auditEvent.AffectedEntity,
            auditEvent.AffectedEntityId,
            auditEvent.OccurredAt,
            auditEvent.ResultingState
        };

        Assert.Equal("AUD-001", observed.AuditEventId.Value);
        Assert.Equal("BATCH-001", observed.BatchNumber!.Value);
        Assert.Equal("OP-001", observed.OperatorId.Value);
        Assert.Equal(AuditEventType.BatchMarkedReady, observed.Action);
        Assert.Equal("Batch", observed.AffectedEntity);
        Assert.Equal("BATCH-001", observed.AffectedEntityId);
        Assert.Equal(Timestamp(1), observed.OccurredAt);
        Assert.Equal("Ready", observed.ResultingState);
    }

    private static DateTimeOffset Timestamp(int hour) =>
        new(2026, 10, 6, hour, 0, 0, TimeSpan.Zero);
}
