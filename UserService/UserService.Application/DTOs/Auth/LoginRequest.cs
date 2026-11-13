using System;

namespace UserService.Application.DTOs.Auth
{
    /// <summary>
    /// Request payload for authenticating a user.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User's registered email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's password in plain text.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
