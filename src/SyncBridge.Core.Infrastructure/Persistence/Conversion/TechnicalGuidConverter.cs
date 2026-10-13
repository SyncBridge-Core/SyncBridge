namespace SyncBridge.Core.Infrastructure.Persistence.Conversion;

/// <summary>
/// Converts persistence-bound technical string identities to and from canonical GUID values.
/// </summary>
public static class TechnicalGuidConverter
{
    /// <summary>
    /// Converts an exact lowercase canonical GUID D string to a nonempty GUID.
    /// </summary>
    /// <param name="value">The technical identifier text.</param>
    /// <returns>The parsed GUID.</returns>
    /// <exception cref="ArgumentException">The value is invalid, empty, or noncanonical.</exception>
    public static Guid ToGuid(string value)
    {
        if (!Guid.TryParseExact(value, "D", out var parsed) ||
            parsed == Guid.Empty ||
            value != parsed.ToString("D"))
        {
            throw new ArgumentException(
                "Technical identifiers must use exact lowercase canonical GUID D text.",
                nameof(value));
        }

        return parsed;
    }

    /// <summary>
    /// Converts a nonempty GUID to lowercase canonical GUID D text.
    /// </summary>
    /// <param name="value">The technical GUID.</param>
    /// <returns>The canonical identifier text.</returns>
    /// <exception cref="ArgumentException">The GUID is empty.</exception>
    public static string ToCanonicalString(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Technical identifiers cannot be empty.", nameof(value));
        }

        return value.ToString("D");
    }
}
