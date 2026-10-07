using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents the verification of equipment for a production batch.
/// </summary>
public sealed class EquipmentVerification
{
    /// <summary>
    /// Initializes equipment-verification evidence for a production Batch.
    /// </summary>
    /// <param name="batchNumber">The associated Batch identity.</param>
    /// <param name="equipmentCode">The verified Equipment identity.</param>
    /// <param name="operatorId">The responsible operator identity.</param>
    /// <param name="verifiedAt">The verification timestamp.</param>
    /// <param name="verificationResult">The verification result.</param>
    public EquipmentVerification(
        BatchNumber batchNumber,
        EquipmentCode equipmentCode,
        OperatorId operatorId,
        DateTimeOffset verifiedAt,
        VerificationResult verificationResult)
    {
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(equipmentCode);
        ArgumentNullException.ThrowIfNull(operatorId);

        BatchNumber = batchNumber;
        EquipmentCode = equipmentCode;
        OperatorId = operatorId;
        VerifiedAt = verifiedAt;
        VerificationResult = verificationResult;
        AuditEvidence = AuditEvent.Create(
            batchNumber,
            operatorId,
            AuditEventType.EquipmentVerified,
            nameof(EquipmentVerification),
            $"{batchNumber.Value}:{equipmentCode.Value}:{operatorId.Value}:{verifiedAt:O}",
            verifiedAt,
            verificationResult.ToString(),
            $"Equipment {equipmentCode.Value}");
    }

    /// <summary>
    /// Gets the associated Batch identity.
    /// </summary>
    public BatchNumber BatchNumber { get; private set; }

    /// <summary>
    /// Gets the verified Equipment identity.
    /// </summary>
    public EquipmentCode EquipmentCode { get; private set; }

    /// <summary>
    /// Gets the responsible operator identity.
    /// </summary>
    public OperatorId OperatorId { get; private set; }

    /// <summary>
    /// Gets the verification timestamp.
    /// </summary>
    public DateTimeOffset VerifiedAt { get; private set; }

    /// <summary>
    /// Gets the verification result.
    /// </summary>
    public VerificationResult VerificationResult { get; private set; }

    /// <summary>
    /// Gets the immutable audit evidence produced for this verification record.
    /// </summary>
    public AuditEvent AuditEvidence { get; }
}
