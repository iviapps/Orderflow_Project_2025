namespace Orderflow.Identity.DTOs.Users.Responses;

/// <summary>
/// Response for password change operation
/// </summary>
public record PasswordChangeResponse
{
    public required string Message { get; init; }
}
