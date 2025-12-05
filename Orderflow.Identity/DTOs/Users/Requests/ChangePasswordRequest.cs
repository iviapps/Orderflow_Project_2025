namespace Orderflow.Identity.DTOs.Users.Requests;

/// <summary>
/// Request model for changing password
/// </summary>
public record ChangePasswordRequest
{
    /// <summary>
    /// Current password
    /// </summary>
    public required string CurrentPassword { get; init; }

    /// <summary>
    /// New password
    /// </summary>
    public required string NewPassword { get; init; }

    /// <summary>
    /// Confirm new password (must match NewPassword)
    /// </summary>
    public required string ConfirmNewPassword { get; init; }
}
