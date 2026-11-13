using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserService.API.Swagger
{
    /// <summary>
    /// Applies JWT Bearer security requirement only to operations requiring authorization.
    /// Honors [Authorize] on controllers/actions and [AllowAnonymous] overrides.
    /// Leaves public endpoints without security requirements.
    /// </summary>
    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

            var hasAuthorize = endpointMetadata?.OfType<IAuthorizeData>().Any() ?? false;
            var hasAllowAnonymous = endpointMetadata?.OfType<IAllowAnonymous>().Any() ?? false;

            if (!hasAuthorize && !hasAllowAnonymous)
            {
                var actionAuth = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();
                var controllerAuth = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false;
                hasAuthorize = actionAuth || controllerAuth;

                var actionAnon = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any();
                var controllerAnon = context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ?? false;
                hasAllowAnonymous = actionAnon || controllerAnon;
            }

            var hasInternalAttr = endpointMetadata?.Any(m => m.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false;
            if (!hasInternalAttr)
            {
                hasInternalAttr = context.MethodInfo.GetCustomAttributes(true).Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute")
                    || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false);
            }

            if (hasAuthorize && !hasAllowAnonymous && !hasInternalAttr)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            }
        }
    }
}

