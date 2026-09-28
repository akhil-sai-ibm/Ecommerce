using System.Threading.Tasks;

namespace UserService.Application.Interfaces.Services
{
    public interface IPasswordHasher
    {
        Task<string> HashPasswordAsync(string password);

        Task<bool> VerifyHashedPasswordAsync(string hashedPassword, string providedPassword);
    }
}
