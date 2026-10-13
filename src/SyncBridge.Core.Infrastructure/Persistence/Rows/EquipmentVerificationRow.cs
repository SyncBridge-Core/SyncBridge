namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational EquipmentVerification record.
/// </summary>
public sealed class EquipmentVerificationRow
{
    /// <summary>Gets or sets the stable verification identity.</summary>
    public Guid VerificationId { get; set; }

    /// <summary>Gets or sets the verified Batch identity.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Gets or sets the verified Equipment identity.</summary>
    public Guid EquipmentId { get; set; }

    /// <summary>Gets or sets whether the equipment was ready.</summary>
    public bool IsReady { get; set; }

    /// <summary>Gets or sets optional calibration-validity evidence.</summary>
    public bool? CalibrationValid { get; set; }

    /// <summary>Gets or sets the responsible user identity.</summary>
    public string VerifiedByUserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the verification timestamp.</summary>
    public DateTimeOffset VerifiedAt { get; set; }

    /// <summary>Gets or sets optional verification comments.</summary>
    public string? Comments { get; set; }
}
