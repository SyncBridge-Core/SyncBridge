namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents persisted equipment reference information.
/// </summary>
public sealed class EquipmentRow
{
    /// <summary>
    /// Gets or sets the equipment identity.
    /// </summary>
    public Guid EquipmentId { get; set; }

    /// <summary>
    /// Gets or sets the unique equipment business code.
    /// </summary>
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the equipment name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional calibration due date.
    /// </summary>
    public DateOnly? CalibrationDueDate { get; set; }
}
