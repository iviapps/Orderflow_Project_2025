using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Orderflow.Identity.Data;
using Orderflow.Identity.Data.Entities;
using Orderflow.Identity.DTOs.Auth;
using Orderflow.Identity.Services.Common;
using Orderflow.Shared.Events;
using System.Security.Claims;

namespace Orderflow.Identity.Services.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IPublishEndpoint publishEndpoint,
        ILogger<GoogleAuthService> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public AuthenticationProperties CreateAuthenticationProperties(string callbackUrl)
    {
        return new AuthenticationProperties
        {
            RedirectUri = callbackUrl,
            Items = { { "LoginProvider", "Google" } }
        };
    }

    public async Task<AuthResult<GoogleLoginResponse>> ProcessCallbackAsync(AuthenticateResult authenticateResult)
    {
        if (!authenticateResult.Succeeded)
        {
            _logger.LogWarning("Google authentication failed: {Error}", 
                authenticateResult.Failure?.Message);
            return AuthResult<GoogleLoginResponse>.Failure("Google authentication failed");
        }

        // Extraer datos de Google
        var claims = authenticateResult.Principal!.Claims;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "";
        var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;

        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Google authentication: No email received");
            return AuthResult<GoogleLoginResponse>.Failure("No email received from Google");
        }

        // Buscar o crear usuario
        var (user, isNewUser) = await FindOrCreateUserAsync(email, googleId, fullName, picture);

        if (user == null)
        {
            return AuthResult<GoogleLoginResponse>.Failure("Failed to create user account");
        }

        // Generar JWT
        var roles = await _userManager.GetRolesAsync(user);
        var token = await _tokenService.GenerateAccessTokenAsync(user, roles);

        _logger.LogInformation("Google OAuth successful for: {Email} (New: {IsNew})", email, isNewUser);

        return AuthResult<GoogleLoginResponse>.Success(new GoogleLoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            IsNewUser = isNewUser
        });
    }

    private async Task<(ApplicationUser? User, bool IsNewUser)> FindOrCreateUserAsync(
        string email, string? googleId, string fullName, string? picture)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            // Crear nuevo usuario
            var nameParts = fullName.Split(' ', 2);
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = nameParts.FirstOrDefault() ?? "",
                LastName = nameParts.Length > 1 ? nameParts[1] : "",
                EmailConfirmed = true,
                GoogleId = googleId,
                ProfilePictureUrl = picture
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to create user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return (null, false);
            }

            await _userManager.AddToRoleAsync(user, Data.Roles.Customer);

            // Publicar evento
            await _publishEndpoint.Publish(new UserRegisteredEvent(
                user.Id, user.Email!, user.FirstName, user.LastName));

            _logger.LogInformation("New user created via Google: {Email}", email);
            return (user, true);
        }

        // Usuario existe - vincular Google si no tiene
        if (string.IsNullOrEmpty(user.GoogleId))
        {
            user.GoogleId = googleId;
            user.ProfilePictureUrl ??= picture;
            await _userManager.UpdateAsync(user);
            _logger.LogInformation("Linked Google to existing user: {Email}", email);
        }

        return (user, false);
    }
}