using System;

namespace UserService.Application.DTOs.Users
{
    public class UpdateUserDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public Guid? RoleId { get; set; }
    }
}
