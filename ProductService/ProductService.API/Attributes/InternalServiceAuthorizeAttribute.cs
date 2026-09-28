using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace ProductService.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class InternalServiceAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var config = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration));
            var expectedKey = config.GetValue<string>("InternalServiceKey");

            if (!context.HttpContext.Request.Headers.TryGetValue("X-Internal-Service-Key", out var providedKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}
