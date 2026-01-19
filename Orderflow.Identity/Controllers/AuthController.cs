using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orderflow.Identity.DTOs.Auth;
using Orderflow.Identity.Services;
using Orderflow.Identity.Services.Auth;

namespace Orderflow.Identity.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly ILogger<AuthController> _logger;
    private readonly string _frontendUrl;

    public AuthController(
        IAuthService authService,
        IGoogleAuthService googleAuthService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
        _logger = logger;
        _frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:5173";
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Succeeded)
            return Unauthorized(result.Errors);
        return Ok(result.Data);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Succeeded)
            return BadRequest(result.Errors);
        return Ok(result.Data);
    }

    // ==================== GOOGLE OAUTH ====================

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin()
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), "Auth", null, Request.Scheme)!;
        var properties = _googleAuthService.CreateAuthenticationProperties(callbackUrl);

        _logger.LogInformation("Initiating Google OAuth flow");
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback()
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            var result = await _googleAuthService.ProcessCallbackAsync(authenticateResult);

            if (!result.Succeeded)
            {
                var error = result.Errors?.FirstOrDefault() ?? "unknown_error";
                return Redirect($"{_frontendUrl}/login?error={error}");
            }

            return Redirect($"{_frontendUrl}/auth/callback?token={result.Data!.Token}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth callback");
            return Redirect($"{_frontendUrl}/login?error=server_error");
        }
    }
}