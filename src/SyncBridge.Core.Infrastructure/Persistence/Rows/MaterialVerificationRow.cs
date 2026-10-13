namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational MaterialVerification record.
/// </summary>
public sealed class MaterialVerificationRow
{
    /// <summary>Gets or sets the stable verification identity.</summary>
    public Guid VerificationId { get; set; }

    /// <summary>Gets or sets the verified Batch identity.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Gets or sets the verified Material identity.</summary>
    public Guid MaterialId { get; set; }

    /// <summary>Gets or sets the optional material lot number.</summary>
    public string? LotNumber { get; set; }

    /// <summary>Gets or sets whether the material was accepted.</summary>
    public bool IsAccepted { get; set; }

    /// <summary>Gets or sets the responsible user identity.</summary>
    public string VerifiedByUserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the verification timestamp.</summary>
    public DateTimeOffset VerifiedAt { get; set; }

    /// <summary>Gets or sets optional verification comments.</summary>
    public string? Comments { get; set; }
}
