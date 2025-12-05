namespace Orderflow.Identity.DTOs.Users.Responses;

/// <summary>
/// Response containing user's roles
/// </summary>
public record UserRolesResponse
{
    public required string UserId { get; init; }
    public required IEnumerable<string> Roles { get; init; }
}
