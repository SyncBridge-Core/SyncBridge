namespace SyncBridge.Core.Domain.ValueObjects;

/// <summary>
/// Represents the immutable identity of an equipment reference record.
/// </summary>
/// <param name="Value">The equipment identity value.</param>
public sealed record EquipmentId(Guid Value);
