using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents the verification of equipment for a production batch.
/// </summary>
public sealed class EquipmentVerification
{
    public EquipmentVerification(
        BatchNumber batchNumber,
        EquipmentCode equipmentCode,
        OperatorId operatorId,
        DateTimeOffset verifiedAt,
        VerificationResult verificationResult)
    {
        BatchNumber = batchNumber;
        EquipmentCode = equipmentCode;
        OperatorId = operatorId;
        VerifiedAt = verifiedAt;
        VerificationResult = verificationResult;
    }

    public BatchNumber BatchNumber { get; private set; }

    public EquipmentCode EquipmentCode { get; private set; }

    public OperatorId OperatorId { get; private set; }

    public DateTimeOffset VerifiedAt { get; private set; }

    public VerificationResult VerificationResult { get; private set; }
}
