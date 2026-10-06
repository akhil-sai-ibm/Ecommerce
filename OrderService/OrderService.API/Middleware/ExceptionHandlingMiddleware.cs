using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Exceptions;

namespace OrderService.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex).ConfigureAwait(false);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            object? result = null;
            int statusCode = (int)HttpStatusCode.InternalServerError;

            switch (exception)
            {
                case DomainException de:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    result = new { error = de.Message };
                    break;
                case ArgumentException ae:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    result = new { error = ae.Message };
                    break;
                case InvalidOperationException ioe:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    result = new { error = ioe.Message };
                    break;
                default:
                    _logger.LogError(exception, "Unhandled exception");
                    result = new { error = "An unexpected error occurred." };
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(result);
            return context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
