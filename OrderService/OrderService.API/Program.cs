using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using OrderService.Infrastructure;
using OrderService.API.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "OrderService API",
        Version = "v1",
        Description = "Order Service API for order processing, checkout, order tracking, and cart management in the ECommerce system."
    });

    var securityScheme = new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your JWT token directly."
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.CustomOperationIds(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues.TryGetValue("controller", out var cVal) && !string.IsNullOrEmpty(cVal)
            ? cVal
            : "Api";
        var action = apiDesc.ActionDescriptor.RouteValues.TryGetValue("action", out var aVal) && !string.IsNullOrEmpty(aVal)
            ? aVal
            : apiDesc.HttpMethod ?? "Action";
        return $"{controller}_{action}";
    });

    c.OperationFilter<OrderService.API.Swagger.AuthorizeCheckOperationFilter>();

    c.OperationFilter<OrderService.API.Swagger.InternalServiceHeaderOperationFilter>();

    var apiXml = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXml);
    if (File.Exists(apiXmlPath))
    {
        c.IncludeXmlComments(apiXmlPath);
    }

    var appXml = $"{typeof(OrderService.Application.DTOs.Order.OrderDto).Assembly.GetName().Name}.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXml);
    if (File.Exists(appXmlPath))
    {
        c.IncludeXmlComments(appXmlPath);
    }
});

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSignalR();
builder.Services.AddHostedService<OrderService.API.BackgroundServices.OutboxProcessorBackgroundService>();
builder.Services.AddHostedService<OrderService.API.BackgroundServices.OrderExpirationBackgroundService>();
builder.Services.AddScoped<OrderService.Application.Interfaces.Services.IOrderNotificationService, OrderService.API.Services.OrderNotificationService>();

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/orderhub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderServiceDbContext>("Database");

var app = builder.Build();

app.UseExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderService.API.Hubs.OrderHub>("/orderhub");

app.MapHealthChecks("/health");

app.Run();
