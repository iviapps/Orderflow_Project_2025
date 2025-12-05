namespace Orderflow.Identity.DTOs.Users.Responses;

/// <summary>
/// Response for role assignment operation
/// </summary>
public record RoleAssignmentResponse
{
    public required string UserId { get; init; }
    public required string RoleName { get; init; }
    public required string Message { get; init; }
}
