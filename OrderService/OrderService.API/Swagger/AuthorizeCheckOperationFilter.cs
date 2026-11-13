using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderService.API.Swagger
{
    /// <summary>
    /// Applies JWT Bearer security requirement only to operations requiring authorization.
    /// Excludes public endpoints, [AllowAnonymous] overrides, and internal service endpoints.
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
                    || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false)
                    || (context.MethodInfo.DeclaringType?.Name == "OrderInternalController");
            }

            var isInternal = hasInternalAttr;

            if (hasAuthorize && !hasAllowAnonymous && !isInternal)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                var schemeDoc = new OpenApiDocument
                {
                    Components = new OpenApiComponents
                    {
                        SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                        {
                            ["Bearer"] = new OpenApiSecurityScheme()
                        }
                    }
                };

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", schemeDoc, null),
                        new List<string>()
                    }
                });
            }
        }
    }
}

