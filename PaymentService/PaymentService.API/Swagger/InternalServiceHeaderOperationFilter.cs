using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PaymentService.API.Swagger
{
    /// <summary>
    /// Adds X-Internal-Service-Key header parameter to internal service endpoints in PaymentService.
    /// </summary>
    public class InternalServiceHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
            var hasInternalAttr = endpointMetadata?.Any(m => m.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false;
            if (!hasInternalAttr)
            {
                hasInternalAttr = context.MethodInfo.GetCustomAttributes(true).Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute")
                    || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).Any(a => a.GetType().Name == "InternalServiceAuthorizeAttribute") ?? false)
                    || (context.MethodInfo.DeclaringType?.Name == "PaymentInternalController");
            }

            if (hasInternalAttr)
            {
                operation.Parameters ??= new List<IOpenApiParameter>();
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-Internal-Service-Key",
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "Secret key required for authenticating internal inter-service requests.",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String
                    }
                });
            }

            if (operation.Parameters != null)
            {
                foreach (var param in operation.Parameters)
                {
                    if (param is OpenApiParameter openApiParam &&
                        string.Equals(param.Name, "Idempotency-Key", StringComparison.OrdinalIgnoreCase))
                    {
                        openApiParam.Required = true;
                    }
                }
            }
        }
    }
}

