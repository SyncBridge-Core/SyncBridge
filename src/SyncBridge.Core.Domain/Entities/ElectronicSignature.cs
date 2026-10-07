using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents immutable electronic-signature evidence for a controlled Batch record.
/// </summary>
public sealed class ElectronicSignature
{
    /// <summary>
    /// Initializes electronic-signature evidence.
    /// </summary>
    /// <param name="electronicSignatureId">The signature identity.</param>
    /// <param name="batchNumber">The associated Batch identity.</param>
    /// <param name="relatedRecordType">The signed controlled-record type.</param>
    /// <param name="relatedRecordId">The signed controlled-record identity.</param>
    /// <param name="operatorId">The signing operator identity.</param>
    /// <param name="meaning">The meaning attributed to the signature.</param>
    /// <param name="signedAt">The signature timestamp.</param>
    public ElectronicSignature(
        ElectronicSignatureId electronicSignatureId,
        BatchNumber batchNumber,
        RelatedRecordType relatedRecordType,
        RelatedRecordId relatedRecordId,
        OperatorId operatorId,
        ElectronicSignatureMeaning meaning,
        DateTimeOffset signedAt)
    {
        ArgumentNullException.ThrowIfNull(electronicSignatureId);
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(relatedRecordType);
        ArgumentNullException.ThrowIfNull(relatedRecordId);
        ArgumentNullException.ThrowIfNull(operatorId);

        if (!Enum.IsDefined(meaning))
        {
            throw new ArgumentOutOfRangeException(nameof(meaning));
        }

        ElectronicSignatureId = electronicSignatureId;
        BatchNumber = batchNumber;
        RelatedRecordType = relatedRecordType;
        RelatedRecordId = relatedRecordId;
        OperatorId = operatorId;
        Meaning = meaning;
        SignedAt = signedAt;
        AuditEvidence = AuditEvent.Create(
            batchNumber,
            operatorId,
            AuditEventType.ElectronicSignatureApplied,
            nameof(ElectronicSignature),
            electronicSignatureId.Value,
            signedAt,
            meaning.ToString(),
            $"Signed {relatedRecordType.Value} {relatedRecordId.Value}");
    }

    /// <summary>
    /// Gets the signature identity.
    /// </summary>
    public ElectronicSignatureId ElectronicSignatureId { get; }

    /// <summary>
    /// Gets the associated Batch identity.
    /// </summary>
    public BatchNumber BatchNumber { get; }

    /// <summary>
    /// Gets the signed controlled-record type.
    /// </summary>
    public RelatedRecordType RelatedRecordType { get; }

    /// <summary>
    /// Gets the signed controlled-record identity.
    /// </summary>
    public RelatedRecordId RelatedRecordId { get; }

    /// <summary>
    /// Gets the signing operator identity.
    /// </summary>
    public OperatorId OperatorId { get; }

    /// <summary>
    /// Gets the meaning attributed to the signature.
    /// </summary>
    public ElectronicSignatureMeaning Meaning { get; }

    /// <summary>
    /// Gets the signature timestamp.
    /// </summary>
    public DateTimeOffset SignedAt { get; }

    /// <summary>
    /// Gets the immutable audit evidence produced for this signature.
    /// </summary>
    public AuditEvent AuditEvidence { get; }
}
