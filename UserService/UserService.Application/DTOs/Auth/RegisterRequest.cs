using System;

namespace UserService.Application.DTOs.Auth
{
    /// <summary>
    /// Request payload for registering a new user account.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// User's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User's email address (used as unique login credential).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Chosen account password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
