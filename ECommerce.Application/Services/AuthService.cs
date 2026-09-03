using System;
using System.Threading.Tasks;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        // Deterministic Customer role GUID seeded earlier
        private static readonly Guid CustomerRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email)) throw new DomainException("Email is required.");
            if (string.IsNullOrWhiteSpace(request.Password)) throw new DomainException("Password is required.");

            var email = request.Email.Trim().ToLowerInvariant();
            var existing = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
            if (existing != null) throw new DomainException("User with this email already exists.");

            var hash = await _passwordHasher.HashPasswordAsync(request.Password).ConfigureAwait(false);

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName?.Trim() ?? string.Empty,
                LastName = request.LastName?.Trim() ?? string.Empty,
                Email = email,
                PasswordHash = hash,
                RoleId = CustomerRoleId,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user).ConfigureAwait(false);
            await _userRepository.SaveChangesAsync().ConfigureAwait(false);

            // add role information for token generation - repository may not have loaded Role; create minimal Role instance
            user.Role = new Role { Id = CustomerRoleId, Name = "Customer" };

            var token = await _tokenService.GenerateTokenAsync(user).ConfigureAwait(false);

            return new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserResponse { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, RoleId = user.RoleId }
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                throw new DomainException("Invalid credentials.");

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(email).ConfigureAwait(false);
            if (user == null) throw new DomainException("Invalid credentials.");

            var valid = await _passwordHasher.VerifyHashedPasswordAsync(user.PasswordHash, request.Password).ConfigureAwait(false);
            if (!valid) throw new DomainException("Invalid credentials.");

            var token = await _tokenService.GenerateTokenAsync(user).ConfigureAwait(false);

            return new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                User = new UserResponse { Id = user.Id, FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, RoleId = user.RoleId }
            };
        }
    }
}
