using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Interfaces.Services;
using UserService.Application.DTOs.Auth;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
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

        [HttpPost("login")]
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
