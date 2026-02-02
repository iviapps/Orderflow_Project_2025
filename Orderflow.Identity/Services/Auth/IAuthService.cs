using System.Security.Claims;                                
using Orderflow.Identity.DTOs.Auth;                          
using Orderflow.Identity.DTOs;                               
using Orderflow.Identity.Services.Common;

<<<<<<< HEAD
namespace Orderflow.Identity.Services;                       
    public interface IAuthService 
=======
namespace Orderflow.Identity.Services {                
    public interface IAuthService
>>>>>>> 4abee25c3f71540d33ac992f5c3162cbcc0e3498
    {
        Task<AuthResult<LoginResponse>> LoginAsync(
            LoginRequest request);                            

        Task<AuthResult<RegisterResponse>> RegisterAsync(
            RegisterRequest request);                         

        Task<AuthResult<CurrentUserResponse>> GetCurrentUserAsync(
            string userId);                                 
    }
