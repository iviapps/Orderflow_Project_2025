using System.Security.Claims;                                
using Orderflow.Identity.DTOs.Auth;                          
using Orderflow.Identity.DTOs;                               
using Orderflow.Identity.Services.Common;

namespace Orderflow.Identity.Services {                
    public interface IAuthService
    {
        Task<AuthResult<LoginResponse>> LoginAsync(
            LoginRequest request);                            

        Task<AuthResult<RegisterResponse>> RegisterAsync(
            RegisterRequest request);                         

        Task<AuthResult<CurrentUserResponse>> GetCurrentUserAsync(
            string userId);                                 
    }
}
