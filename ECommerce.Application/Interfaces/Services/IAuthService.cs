using System.Threading.Tasks;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
