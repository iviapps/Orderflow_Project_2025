namespace Orderflow.Identity.DTOs.Users.Requests;

/// <summary>
/// Request model for updating own profile (self-management)
/// </summary>
public record UpdateProfileRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// Phone number (nullable)
    /// </summary>
    public string? PhoneNumber { get; init; }
}
