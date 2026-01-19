using Microsoft.AspNetCore.Authentication;
using Orderflow.Identity.DTOs.Auth;
using Orderflow.Identity.Services.Common;

namespace Orderflow.Identity.Services.Auth;

public interface IGoogleAuthService
{
    /// <summary>
    /// Creates authentication properties to initiate Google OAuth flow
    /// </summary>
    AuthenticationProperties CreateAuthenticationProperties(string callbackUrl);

    /// <summary>
    /// Processes the Google OAuth callback and returns a JWT token
    /// </summary>
    Task<AuthResult<GoogleLoginResponse>> ProcessCallbackAsync(AuthenticateResult authenticateResult);
}