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

        // Profile Management
        Task<UserProfileDto?> GetUserProfileAsync(Guid userId);
        Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto dto);
        Task<string> UploadProfileImageAsync(Guid userId, ECommerce.Application.DTOs.Common.FileData file);
        Task DeleteProfileImageAsync(Guid userId);
    }
}
