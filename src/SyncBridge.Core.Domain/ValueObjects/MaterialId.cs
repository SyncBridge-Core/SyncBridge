namespace SyncBridge.Core.Domain.ValueObjects;

/// <summary>
/// Represents the immutable identity of a material reference record.
/// </summary>
/// <param name="Value">The material identity value.</param>
public sealed record MaterialId(Guid Value);
