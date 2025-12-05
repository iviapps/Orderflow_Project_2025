namespace Orderflow.Identity.DTOs.Roles.Responses;

/// <summary>
/// Response model for role information
/// </summary>
public record RoleResponse
{
    /// <summary>
    /// Role ID
    /// </summary>
    public required string RoleId { get; init; }

    /// <summary>
    /// Role name
    /// </summary>
    public required string RoleName { get; init; }

    /// <summary>
    /// Normalized role name (uppercase)
    /// </summary>
    public required string NormalizedName { get; init; }

    /// <summary>
    /// Number of users with this role
    /// </summary>
    public int UserCount { get; init; }
}
