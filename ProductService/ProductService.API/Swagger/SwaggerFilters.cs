using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ProductService.API.Swagger
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
                    || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false);
            }

            var isInternal = hasInternalAttr;

            if (hasAuthorize && !hasAllowAnonymous && !isInternal)
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

    /// <summary>
    /// Adds X-Internal-Service-Key header documentation to internal service endpoints.
    /// </summary>
    public class InternalServiceHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            var hasInternalAttr = endpointMetadata?.Any(m => m.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false;
            if (!hasInternalAttr)
            {
                hasInternalAttr = context.MethodInfo.GetCustomAttributes(true)
                    .Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute")
                    || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                        .Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false);
            }

            if (hasInternalAttr)
            {
                operation.Parameters ??= new List<OpenApiParameter>();
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Internal-Service-Key",
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Secret key required for authenticating internal inter-service requests.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });
            }
        }
    }

    /// <summary>
    /// Adds X-Total-Count response header documentation to paged search endpoints.
    /// </summary>
    public class TotalCountHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var path = context.ApiDescription.RelativePath ?? string.Empty;
            if (path.Equals("api/Products/search", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("api/products/search", StringComparison.OrdinalIgnoreCase))
            {
                if (operation.Responses.TryGetValue("200", out var response))
                {
                    response.Headers ??= new Dictionary<string, OpenApiHeader>();
                    response.Headers["X-Total-Count"] = new OpenApiHeader
                    {
                        Description = "Total count of matching products across all pages for pagination calculation.",
                        Schema = new OpenApiSchema
                        {
                            Type = "integer"
                        }
                    };
                }
            }
        }
    }
}

