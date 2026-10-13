using SyncBridge.Core.Domain.Enumerations;

namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the relational ElectronicSignature record.
/// </summary>
public sealed class ElectronicSignatureRow
{
    /// <summary>Gets or sets the immutable signature identity.</summary>
    public Guid SignatureId { get; set; }

    /// <summary>Gets or sets the associated Batch identity.</summary>
    public Guid BatchId { get; set; }

    /// <summary>Gets or sets the controlled-record discriminator.</summary>
    public string RelatedRecordType { get; set; } = string.Empty;

    /// <summary>Gets or sets the controlled-record identity.</summary>
    public Guid RelatedRecordId { get; set; }

    /// <summary>Gets or sets the signing user identity.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the signature meaning.</summary>
    public ElectronicSignatureMeaning Meaning { get; set; }

    /// <summary>Gets or sets the signing timestamp.</summary>
    public DateTimeOffset SignedAt { get; set; }
}
