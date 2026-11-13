using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Users;
using UserService.Application.Interfaces.Services;
using UserService.Domain.Exceptions;

namespace UserService.API.Controllers
{
    /// <summary>
    /// Manages user profile retrieval, profile updates, and avatar image uploads.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Retrieves the current authenticated user's profile details.
        /// </summary>
        /// <returns>User profile including identifier, name, email, phone, and role information.</returns>
        /// <response code="200">User profile successfully retrieved.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="404">User profile not found.</response>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized();

            var profile = await _userService.GetUserProfileAsync(userId);
            if (profile == null)
                return NotFound("User not found.");

            return Ok(profile);
        }

        /// <summary>
        /// Updates the current authenticated user's profile details (first name, last name, phone).
        /// </summary>
        /// <param name="dto">Updated user profile information.</param>
        /// <response code="204">Profile updated successfully.</response>
        /// <response code="400">Validation error or invalid input data.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpPut("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized();

            try
            {
                await _userService.UpdateUserProfileAsync(userId, dto);
                return NoContent();
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Uploads an avatar image for the current authenticated user.
        /// </summary>
        /// <param name="file">Image file (.jpg, .jpeg, .png).</param>
        /// <returns>Object containing the public URL of the uploaded image.</returns>
        /// <response code="200">Image uploaded successfully.</response>
        /// <response code="400">No file uploaded, invalid file format, or validation failure.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        /// <response code="415">Request payload is not multipart/form-data.</response>
        [HttpPost("profile/image")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized();

            try
            {
                using var stream = file.OpenReadStream();
                var fileData = new FileData
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Content = stream,
                    Length = file.Length
                };

                var newUrl = await _userService.UploadProfileImageAsync(userId, fileData);
                return Ok(new { url = newUrl });
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Deletes the avatar image for the current authenticated user.
        /// </summary>
        /// <response code="204">Profile image removed successfully.</response>
        /// <response code="400">Operation failure or image could not be deleted.</response>
        /// <response code="401">Missing or invalid authentication token.</response>
        [HttpDelete("profile/image")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out Guid userId))
                return Unauthorized();

            try
            {
                await _userService.DeleteProfileImageAsync(userId);
                return NoContent();
            }
            catch (DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
