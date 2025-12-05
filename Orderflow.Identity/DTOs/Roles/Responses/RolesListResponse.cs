namespace Orderflow.Identity.DTOs.Roles.Responses;


/// <summary>
/// Response containing list of all roles
/// </summary>
public record RolesListResponse
{
    public required IEnumerable<RoleResponse> Roles { get; init; }
}
