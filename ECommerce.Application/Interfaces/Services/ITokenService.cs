using System.Threading.Tasks;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces.Services
{
    public interface ITokenService
    {
        Task<string> GenerateTokenAsync(User user);
    }
}
