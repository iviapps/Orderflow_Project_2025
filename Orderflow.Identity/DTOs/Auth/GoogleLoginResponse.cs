namespace Orderflow.Identity.DTOs.Auth;

public class GoogleLoginResponse
{
    public required string Token { get; set; }
    public required string UserId { get; set; }
    public required string Email { get; set; }
    public bool IsNewUser { get; set; }
}