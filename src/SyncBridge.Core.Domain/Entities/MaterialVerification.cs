using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents the verification of a material for a production batch.
/// </summary>
public sealed class MaterialVerification
{
    public MaterialVerification(
        BatchNumber batchNumber,
        MaterialCode materialCode,
        OperatorId operatorId,
        DateTimeOffset verifiedAt,
        VerificationResult verificationResult)
    {
        BatchNumber = batchNumber;
        MaterialCode = materialCode;
        OperatorId = operatorId;
        VerifiedAt = verifiedAt;
        VerificationResult = verificationResult;
    }

    public BatchNumber BatchNumber { get; private set; }

    public MaterialCode MaterialCode { get; private set; }

    public OperatorId OperatorId { get; private set; }

    public DateTimeOffset VerifiedAt { get; private set; }

    public VerificationResult VerificationResult { get; private set; }
}
