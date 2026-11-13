using System.ComponentModel.DataAnnotations;

namespace UserService.Application.DTOs.Users
{
    /// <summary>
    /// Request payload for updating the current user's profile details.
    /// </summary>
    public class UpdateUserProfileDto
    {
        /// <summary>
        /// Updated first name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Updated last name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Updated contact phone number.
        /// </summary>
        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
