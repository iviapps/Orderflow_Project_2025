namespace Orderflow.Identity.DTOs.Users.Requests;

/// <summary>
/// Request model for creating a new user (admin operation)
/// </summary>
public record CreateUserRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User password
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// Username (optional, defaults to email if not provided)
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Roles to assign to user (optional, defaults to ["Customer"])
    /// </summary>
    public IEnumerable<string>? Roles { get; init; }
}
