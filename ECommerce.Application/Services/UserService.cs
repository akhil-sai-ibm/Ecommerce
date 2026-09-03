using System;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
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

        private static UserDto Map(User u) => new UserDto { Id = u.Id, FirstName = u.FirstName, LastName = u.LastName, Email = u.Email, RoleId = u.RoleId, CreatedAt = u.CreatedAt };
    }
}
