using System;
using System.Collections.Generic;

namespace UserService.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Do not store plaintext passwords
        public string PasswordHash { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }

        public string? PhoneNumber { get; set; }

        public Guid RoleId { get; set; }

        public Role? Role { get; set; }

        public ICollection<Address> Addresses { get; set; } = new List<Address>();
    }
}
