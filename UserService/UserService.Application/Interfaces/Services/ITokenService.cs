using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace UserService.Application.Interfaces.Services
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(User user);
    }
}
