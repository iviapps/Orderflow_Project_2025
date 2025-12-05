namespace Orderflow.Identity.DTOs.Roles.Requests;

/// <summary>
/// Request model for creating a new role
/// </summary>
public record CreateRoleRequest
{
    /// <summary>
    /// Role name
    /// </summary>
    public required string RoleName { get; init; }
}
