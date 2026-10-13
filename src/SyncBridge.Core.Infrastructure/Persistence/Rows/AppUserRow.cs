namespace SyncBridge.Core.Infrastructure.Persistence.Rows;

/// <summary>
/// Represents the persisted application-user identity used by manufacturing evidence.
/// </summary>
public sealed class AppUserRow
{
    /// <summary>
    /// Gets or sets the stable authenticated user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique login or user name.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name used in manufacturing records.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; }
}
