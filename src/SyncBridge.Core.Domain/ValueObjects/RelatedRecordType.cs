namespace SyncBridge.Core.Domain.ValueObjects;

/// <summary>
/// Represents the type of controlled record associated with an electronic signature.
/// </summary>
public sealed record RelatedRecordType
{
    /// <summary>
    /// Initializes a controlled-record type.
    /// </summary>
    /// <param name="value">The record-type value.</param>
    public RelatedRecordType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Related record type is required.", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the BatchStepExecution record type.
    /// </summary>
    public static RelatedRecordType BatchStepExecution { get; } = new("BatchStepExecution");

    /// <summary>
    /// Gets the record-type value.
    /// </summary>
    public string Value { get; }
}
