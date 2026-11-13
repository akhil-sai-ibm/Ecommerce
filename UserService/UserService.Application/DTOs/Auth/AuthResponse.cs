using System;

namespace UserService.Application.DTOs.Auth
{
    /// <summary>
    /// Response returned upon successful user authentication or registration.
    /// </summary>
    public class AuthResponse
    {
        /// <summary>
        /// JWT bearer access token for authorized requests.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Expiration timestamp of the access token in UTC.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Basic identity profile of the authenticated user.
        /// </summary>
        public UserResponse User { get; set; } = new UserResponse();
    }
}
