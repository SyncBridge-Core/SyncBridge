using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents the verification of a material for a production batch.
/// </summary>
public sealed class MaterialVerification
{
    /// <summary>
    /// Initializes material-verification evidence for a production Batch.
    /// </summary>
    /// <param name="batchNumber">The associated Batch identity.</param>
    /// <param name="materialCode">The verified Material identity.</param>
    /// <param name="operatorId">The responsible operator identity.</param>
    /// <param name="verifiedAt">The verification timestamp.</param>
    /// <param name="verificationResult">The verification result.</param>
    public MaterialVerification(
        BatchNumber batchNumber,
        MaterialCode materialCode,
        OperatorId operatorId,
        DateTimeOffset verifiedAt,
        VerificationResult verificationResult)
    {
        ArgumentNullException.ThrowIfNull(batchNumber);
        ArgumentNullException.ThrowIfNull(materialCode);
        ArgumentNullException.ThrowIfNull(operatorId);

        BatchNumber = batchNumber;
        MaterialCode = materialCode;
        OperatorId = operatorId;
        VerifiedAt = verifiedAt;
        VerificationResult = verificationResult;
        AuditEvidence = AuditEvent.Create(
            batchNumber,
            operatorId,
            AuditEventType.MaterialVerified,
            nameof(MaterialVerification),
            $"{batchNumber.Value}:{materialCode.Value}:{operatorId.Value}:{verifiedAt:O}",
            verifiedAt,
            verificationResult.ToString(),
            $"Material {materialCode.Value}");
    }

    /// <summary>
    /// Gets the associated Batch identity.
    /// </summary>
    public BatchNumber BatchNumber { get; private set; }

    /// <summary>
    /// Gets the verified Material identity.
    /// </summary>
    public MaterialCode MaterialCode { get; private set; }

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
