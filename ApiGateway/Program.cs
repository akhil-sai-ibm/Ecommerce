using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.Extensions.Configuration;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Load ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        corsBuilder => corsBuilder
            .WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000") // standard React/Vite ports
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Total-Count")
            .AllowCredentials());
});

builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("CorsPolicy");

// Security middleware to drop internal service key
app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey("X-Internal-Service-Key"))
    {
        context.Request.Headers.Remove("X-Internal-Service-Key");
    }
    await next();
});

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/health");
});

app.UseWebSockets();
app.UseOcelot().Wait();

app.Run();
