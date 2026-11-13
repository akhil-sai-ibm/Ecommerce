using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Auth;
using UserService.Application.Interfaces.Services;

namespace UserService.API.Controllers
{
    /// <summary>
    /// Handles authentication requests such as user registration and login.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user account with default customer role.
        /// </summary>
        /// <param name="request">Registration credentials and user information.</param>
        /// <returns>Authentication response containing JWT token, expiration timestamp, and user summary.</returns>
        /// <response code="201">User registered successfully.</response>
        /// <response code="400">Validation failure or email already in use.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request).ConfigureAwait(false);
                return CreatedAtAction(null, result);
            }
            catch (UserService.Domain.Exceptions.DomainException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Authenticates user credentials and generates a JWT access token.
        /// </summary>
        /// <param name="request">User email and password.</param>
        /// <returns>Authentication response containing JWT token and user profile.</returns>
        /// <response code="200">Authentication successful.</response>
        /// <response code="401">Invalid credentials provided.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.LoginAsync(request).ConfigureAwait(false);
                return Ok(result);
            }
            catch (UserService.Domain.Exceptions.DomainException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
