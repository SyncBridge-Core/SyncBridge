namespace SyncBridge.Core.Domain.ValueObjects;

/// <summary>
/// Represents the immutable identity of a production order.
/// </summary>
/// <param name="Value">The production-order identity value.</param>
public sealed record ProductionOrderId(Guid Value);
