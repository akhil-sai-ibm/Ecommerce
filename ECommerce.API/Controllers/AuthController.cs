using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.DTOs.Auth;

namespace ECommerce.API.Controllers
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
            var result = await _authService.RegisterAsync(request).ConfigureAwait(false);
            return CreatedAtAction(null, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request).ConfigureAwait(false);
            return Ok(result);
        }
    }
}
