using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents equipment reference information used by manufacturing verification.
/// </summary>
public sealed class Equipment
{
    /// <summary>
    /// Initializes an equipment reference record.
    /// </summary>
    /// <param name="equipmentId">The equipment identity.</param>
    /// <param name="equipmentCode">The unique equipment business code.</param>
    /// <param name="name">The equipment name or description.</param>
    /// <param name="calibrationDueDate">The optional calibration due date.</param>
    public Equipment(
        EquipmentId equipmentId,
        EquipmentCode equipmentCode,
        string name,
        DateOnly? calibrationDueDate = null)
    {
        ArgumentNullException.ThrowIfNull(equipmentId);
        ArgumentNullException.ThrowIfNull(equipmentCode);

        if (equipmentId.Value == Guid.Empty)
        {
            throw new ArgumentException("Equipment identity is required.", nameof(equipmentId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Equipment name is required.", nameof(name));
        }

        EquipmentId = equipmentId;
        EquipmentCode = equipmentCode;
        Name = name;
        CalibrationDueDate = calibrationDueDate;
    }

    /// <summary>Gets the equipment identity.</summary>
    public EquipmentId EquipmentId { get; }

    /// <summary>Gets the unique equipment business code.</summary>
    public EquipmentCode EquipmentCode { get; }

    /// <summary>Gets the equipment name or description.</summary>
    public string Name { get; }

    /// <summary>Gets the optional calibration due date.</summary>
    public DateOnly? CalibrationDueDate { get; }
}
