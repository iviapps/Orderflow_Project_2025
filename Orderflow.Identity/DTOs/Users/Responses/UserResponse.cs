namespace Orderflow.Identity.DTOs.Users.Responses;

/// <summary>
/// Response model for user information (list view)
/// </summary>
public record UserResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// User email
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Username
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Email confirmed status
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// Lockout end date (null if not locked)
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; init; }

    /// <summary>
    /// Lockout enabled
    /// </summary>
    public bool LockoutEnabled { get; init; }

    /// <summary>
    /// Failed access attempts count
    /// </summary>
    public int AccessFailedCount { get; init; }

    /// <summary>
    /// User roles
    /// </summary>
    public required IEnumerable<string> Roles { get; init; }
}
