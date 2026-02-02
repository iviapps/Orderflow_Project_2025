using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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

            // Igual que arriba: usar Succeeded
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }
    }
}
