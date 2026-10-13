namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents persisted material reference information.
/// </summary>
public sealed class MaterialRow
{
    /// <summary>
    /// Gets or sets the material identity.
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Gets or sets the unique material business code.
    /// </summary>
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the material name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
