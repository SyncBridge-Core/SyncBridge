namespace SyncBridge.Core.Infrastructure.Persistence.Exceptions;

/// <summary>
/// Represents persisted data that cannot be reconstructed without violating approved structural rules.
/// </summary>
public sealed class PersistenceIntegrityException : Exception
{
    /// <summary>
    /// Initializes a persistence-integrity failure.
    /// </summary>
    /// <param name="message">The integrity failure description.</param>
    public PersistenceIntegrityException(string message)
        : base(message)
    {
    }
}
