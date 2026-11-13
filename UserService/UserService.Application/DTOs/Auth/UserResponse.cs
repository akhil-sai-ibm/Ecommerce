using System;

namespace UserService.Application.DTOs.Auth
{
    /// <summary>
    /// User identity summary returned in authentication responses.
    /// </summary>
    public class UserResponse
    {
        /// <summary>
        /// Unique user identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Assigned role identifier.
        /// </summary>
        public Guid RoleId { get; set; }
    }
}
