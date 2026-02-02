using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Orderflow.Identity.DTOs.Auth;
using Orderflow.Identity.Services;
using Orderflow.Identity.Services.Auth;

namespace Orderflow.Identity.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly ILogger<AuthController> _logger;
        private readonly string _frontendUrl;
        private readonly bool _googleAuthEnabled;

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

            // Verificar si Google OAuth está configurado
            _googleAuthEnabled = !string.IsNullOrEmpty(configuration["Google:ClientId"])
                              && !string.IsNullOrEmpty(configuration["Google:ClientSecret"]);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Llamamos al servicio de autenticación
            var result = await _authService.LoginAsync(request);

            // OJO: la propiedad se llama Succeeded, no Success
            if (!result.Succeeded)
                // Puedes devolver directamente los errores o envolverlos en tu ErrorResponse
                return Unauthorized(result.Errors);

            // Si todo va bien, devolvemos el payload (Data) o el result entero, como prefieras
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

        /// <summary>
        /// Verifica si Google OAuth está habilitado
        /// </summary>
        [HttpGet("google/status")]
        public IActionResult GoogleStatus()
        {
            return Ok(new { enabled = _googleAuthEnabled });
        }

        /// <summary>
        /// Inicia el flujo de autenticación con Google.
        /// Redirige al usuario a la página de login de Google.
        /// </summary>
        [HttpGet("google/login")]
        public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
        {
            if (!_googleAuthEnabled)
            {
                _logger.LogWarning("Google OAuth not configured");
                return BadRequest(new { error = "Google authentication is not configured" });
            }

            // La URL a donde Google redirigirá después del login
            var callbackUrl = Url.Action(nameof(GoogleCallback), "Auth", new { returnUrl }, Request.Scheme);

            var properties = _googleAuthService.CreateAuthenticationProperties(callbackUrl!);

            // Challenge redirige al proveedor de OAuth (Google)
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// Callback que Google llama después de la autenticación.
        /// Procesa el resultado y genera un JWT token.
        /// </summary>
        [HttpGet("google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string? returnUrl = null)
        {
            // Autenticar usando el esquema externo (Google)
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                _logger.LogWarning("Google authentication failed: {Error}",
                    authenticateResult.Failure?.Message ?? "Unknown error");

                // Redirigir al frontend con error
                return Redirect($"{_frontendUrl}/login?error=google_auth_failed");
            }

            // Procesar el callback y crear/vincular usuario
            var result = await _googleAuthService.ProcessCallbackAsync(authenticateResult);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to process Google callback: {Errors}",
                    string.Join(", ", result.Errors));

                return Redirect($"{_frontendUrl}/login?error=google_processing_failed");
            }

            // Redirigir al frontend con el token
            // El frontend recibirá el token y lo guardará en localStorage/sessionStorage
            var data = result.Data!;
            var redirectUrl = string.IsNullOrEmpty(returnUrl)
                ? $"{_frontendUrl}/auth/callback?token={data.Token}&userId={data.UserId}&isNewUser={data.IsNewUser}"
                : $"{returnUrl}?token={data.Token}&userId={data.UserId}&isNewUser={data.IsNewUser}";

            _logger.LogInformation("Google OAuth successful for {Email}, redirecting to frontend", data.Email);

            return Redirect(redirectUrl);
        }
    }
}
