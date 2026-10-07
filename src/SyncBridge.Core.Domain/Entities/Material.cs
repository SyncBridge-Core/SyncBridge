using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Entities;

/// <summary>
/// Represents material reference information used by manufacturing verification.
/// </summary>
public sealed class Material
{
    /// <summary>
    /// Initializes a material reference record.
    /// </summary>
    /// <param name="materialId">The material identity.</param>
    /// <param name="materialCode">The unique material business code.</param>
    /// <param name="name">The material name or description.</param>
    public Material(MaterialId materialId, MaterialCode materialCode, string name)
    {
        ArgumentNullException.ThrowIfNull(materialId);
        ArgumentNullException.ThrowIfNull(materialCode);

        if (materialId.Value == Guid.Empty)
        {
            throw new ArgumentException("Material identity is required.", nameof(materialId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Material name is required.", nameof(name));
        }

        MaterialId = materialId;
        MaterialCode = materialCode;
        Name = name;
    }

    /// <summary>Gets the material identity.</summary>
    public MaterialId MaterialId { get; }

    /// <summary>Gets the unique material business code.</summary>
    public MaterialCode MaterialCode { get; }

    /// <summary>Gets the material name or description.</summary>
    public string Name { get; }
}
