using System;

namespace UserService.Application.DTOs.Users
{
    /// <summary>
    /// User profile details returned for authenticated profile queries.
    /// </summary>
    public class UserProfileDto
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
        /// User's registered email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Contact phone number, if provided.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Public URL or relative path to the profile avatar image.
        /// </summary>
        public string? ProfileImageUrl { get; set; }

        /// <summary>
        /// Assigned role identifier.
        /// </summary>
        public Guid RoleId { get; set; }

        /// <summary>
        /// Assigned role name (e.g., Customer, Admin).
        /// </summary>
        public string? RoleName { get; set; }

        /// <summary>
        /// Account creation timestamp in UTC.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
