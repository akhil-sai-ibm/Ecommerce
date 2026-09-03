using System;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Users;

namespace ECommerce.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto?> GetByEmailAsync(string email);
        Task<UserDto> RegisterAsync(RegisterUserDto dto);
        Task UpdateAsync(Guid id, UpdateUserDto dto);
    }
}
