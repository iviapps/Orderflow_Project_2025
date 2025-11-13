using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Overflow.Identity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(
            ILogger<UsersController> logger,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        [HttpPost("create")]
        //Task y Action result para devolver distintas response 
        public async Task<ActionResult<UserCreationResponse>> CreateUserAsync(UserCreationRequest request)
        {
            var user = new IdentityUser
            {
                UserName = request.Email,
                Email = request.Email
            };

            // Llamada asíncrona real, sin .Result 
            // Preferible 
            var result = await _userManager.CreateAsync(user, request.Password);

            // Caso: fallo al crear usuario
            if (!result.Succeeded)
            {
                _logger.LogError(
                    "User creation failed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description))
                );
                // Devolvemos los errores en la respuesta <- Con action result con BadRequest tras return
                return BadRequest(new UserCreationResponse
                {
                    Email = request.Email,
                    Message = "User creation failed",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }

            // Caso: éxito
            _logger.LogInformation("User created successfully: {Email}", request.Email);

            // Devolvemos la respuesta de éxito <- por action result con Ok tras return 
            return Ok(new UserCreationResponse
            {
                Email = request.Email,
                Message = "User created successfully"
            });
        }
    }

    //action result para devolver distintas response ???? 
    public class UserCreationResponse
    {
        // Intentionally left empty  
        //Para inicializar propiedades que no quieres que sean null <- = string.Empty
        public required string Email { get; set; }
        //en mayuscula porque es publica 
        public required string Message { get; set; }

        // Puede ser null si no hay errores, podemos llamarlo en !result.SUCCEEDED de forma que nos imprimira una descripcion de errores. 
        public IEnumerable<string>? Errors { get; set; }
    }

    public class UserCreationRequest
    {
        public required string Email { get; set; }  // ← Cambiado de field a property
        public required string Password { get; set; }
    }
}
