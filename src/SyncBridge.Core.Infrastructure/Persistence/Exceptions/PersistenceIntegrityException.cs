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
    /// <param name="innerException">The original reconstruction failure for diagnostic logging.</param>
    public PersistenceIntegrityException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
