using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ECommerce.Application.Interfaces.Services;

namespace ECommerce.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User?.FindFirst(ClaimTypes.Name)?.Value;
                if (Guid.TryParse(sub, out var id)) return id;
                return null;
            }
        }

        public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

        public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    }
}
