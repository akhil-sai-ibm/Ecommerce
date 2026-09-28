using System;
using System.IO;
using System.Threading.Tasks;
using UserService.Application.DTOs.Users;
using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces.Repositories;
using UserService.Application.Interfaces.Services;
using UserService.Application.Utils;
using UserService.Domain.Entities;
using UserService.Domain.Exceptions;

namespace UserService.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFileStorageService _fileStorageService;

        public UserService(IUserRepository userRepository, IFileStorageService fileStorageService)
        {
            _userRepository = userRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var u = await _userRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (u == null) return null;
            return Map(u);
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var u = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
            if (u == null) return null;
            return Map(u);
        }

        public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email)) throw new DomainException("Email is required.");
            if (string.IsNullOrWhiteSpace(dto.Password)) throw new DomainException("Password is required.");

            var exists = await _userRepository.GetByEmailAsync(dto.Email.Trim()).ConfigureAwait(false);
            if (exists != null) throw new DomainException("Email already registered.");

            // Password hashing not implemented yet: store placeholder hash indicator (do not store plain text in production)
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName?.Trim() ?? string.Empty,
                LastName = dto.LastName?.Trim() ?? string.Empty,
                Email = dto.Email.Trim(),
                PasswordHash = "", // Replace with actual hash when available
                RoleId = dto.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user).ConfigureAwait(false);
            await _userRepository.SaveChangesAsync().ConfigureAwait(false);

            return Map(user);
        }

        public async Task UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (user == null) throw new DomainException("User not found.");

            if (!string.IsNullOrWhiteSpace(dto.FirstName)) user.FirstName = dto.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.LastName)) user.LastName = dto.LastName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.Trim() != user.Email)
            {
                var exists = await _userRepository.GetByEmailAsync(dto.Email.Trim()).ConfigureAwait(false);
                if (exists != null && exists.Id != id) throw new DomainException("Email already in use.");
                user.Email = dto.Email.Trim();
            }

            if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetWithRoleAsync(userId).ConfigureAwait(false);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task UpdateUserProfileAsync(Guid userId, UpdateUserProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
            if (user == null) throw new DomainException("User not found.");

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            
            if (dto.PhoneNumber != null)
                user.PhoneNumber = dto.PhoneNumber.Trim();

            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<string> UploadProfileImageAsync(Guid userId, FileData file)
        {
            var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
            if (user == null) throw new DomainException("User not found.");

            // Validate magic bytes
            if (!ImageValidator.IsValidImage(file.Content))
            {
                throw new DomainException("Invalid image format. Supported formats are JPG, PNG, WebP.");
            }

            var oldImageUrl = user.ProfileImageUrl;

            var newUrl = await _fileStorageService.SaveFileAsync(file, "profiles").ConfigureAwait(false);

            user.ProfileImageUrl = newUrl;
            user.UpdatedAt = DateTime.UtcNow;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync().ConfigureAwait(false);

            if (!string.IsNullOrEmpty(oldImageUrl))
            {
                await _fileStorageService.DeleteFileAsync(oldImageUrl).ConfigureAwait(false);
            }

            return newUrl;
        }

        public async Task DeleteProfileImageAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false);
            if (user == null) throw new DomainException("User not found.");

            if (!string.IsNullOrEmpty(user.ProfileImageUrl))
            {
                await _fileStorageService.DeleteFileAsync(user.ProfileImageUrl).ConfigureAwait(false);
                user.ProfileImageUrl = null;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private static UserDto Map(User u) => new UserDto { Id = u.Id, FirstName = u.FirstName, LastName = u.LastName, Email = u.Email, RoleId = u.RoleId, CreatedAt = u.CreatedAt };
    }
}
