using System;

namespace UserService.Application.DTOs.Users
{
    public class RegisterUserDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // plaintext here only in DTO; hashing to be done by infrastructure/service
        public Guid RoleId { get; set; }
    }
}
