namespace SyncBridge.Core.Infrastructure.Persistence.Validation;

/// <summary>
/// Validates string values against DBDS UTF-16 storage limits without truncation.
/// </summary>
public static class DbdsStringLengthValidator
{
    /// <summary>
    /// Requires a nonblank value whose UTF-16 code-unit count does not exceed the supplied limit.
    /// </summary>
    /// <param name="value">The persistence-bound value.</param>
    /// <param name="maximumLength">The approved DBDS maximum length.</param>
    /// <param name="parameterName">The source parameter name.</param>
    /// <returns>The unchanged validated value.</returns>
    public static string RequireWithinLimit(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (maximumLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A persistence value is required.", parameterName);
        }

        if (value.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value exceeds the approved {maximumLength}-code-unit DBDS limit.",
                parameterName);
        }

        return value;
    }
}
