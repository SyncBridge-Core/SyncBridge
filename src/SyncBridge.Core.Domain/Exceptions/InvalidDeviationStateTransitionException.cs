using SyncBridge.Core.Domain.Enumerations;
using SyncBridge.Core.Domain.ValueObjects;

namespace SyncBridge.Core.Domain.Exceptions;

/// <summary>
/// Represents an invalid deviation lifecycle operation.
/// </summary>
public sealed class InvalidDeviationStateTransitionException : DomainException
{
    /// <summary>
    /// Initializes an invalid deviation state-transition exception.
    /// </summary>
    /// <param name="deviationCode">The affected deviation identity.</param>
    /// <param name="currentStatus">The deviation's current status.</param>
    /// <param name="operation">The rejected operation.</param>
    public InvalidDeviationStateTransitionException(
        DeviationCode deviationCode,
        DeviationStatus currentStatus,
        string operation)
        : base($"Deviation '{deviationCode.Value}' in status '{currentStatus}' cannot {operation}.")
    {
        DeviationCode = deviationCode;
        CurrentStatus = currentStatus;
    }

    /// <summary>
    /// Gets the affected deviation identity.
    /// </summary>
    public DeviationCode DeviationCode { get; }

    /// <summary>
    /// Gets the status from which the operation was rejected.
    /// </summary>
    public DeviationStatus CurrentStatus { get; }
}
