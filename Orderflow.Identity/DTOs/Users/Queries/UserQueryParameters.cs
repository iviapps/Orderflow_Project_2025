namespace Orderflow.Identity.DTOs.Users.Queries;

/// <summary>
/// Query parameters for filtering and paginating users
/// </summary>
public record UserQueryParameters
{
    /// <summary>
    /// Page number (starts at 1)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (items per page, max 100)
    /// </summary>
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Search term (searches email and username, case-insensitive)
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Filter by role name
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Sort field (email, userName). Optional - defaults to email if not specified.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort in descending order. Optional - defaults to false (ascending) if not specified.
    /// </summary>
    public bool? SortDescending { get; init; }
}
