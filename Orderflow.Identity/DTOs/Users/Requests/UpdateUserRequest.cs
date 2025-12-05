namespace Orderflow.Identity.DTOs.Users.Requests;

/// <summary>
/// Request model for updating user information (admin operation)
/// </summary>
public record UpdateUserRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Username
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Phone number (nullable)
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Email confirmed status
    /// </summary>
    public bool EmailConfirmed { get; init; }

    /// <summary>
    /// Lockout enabled
    /// </summary>
    public bool LockoutEnabled { get; init; }
}
