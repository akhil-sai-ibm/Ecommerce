using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PaymentService.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Application;
using PaymentService.Infrastructure;
using PaymentService.API.BackgroundServices;
using PaymentService.API.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "PaymentService API",
        Version = "v1",
        Description = "Payment Service API for payment processing, verification, and refunds in the ECommerce system."
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

    c.OperationFilter<PaymentService.API.Swagger.AuthorizeCheckOperationFilter>();

    c.OperationFilter<PaymentService.API.Swagger.InternalServiceHeaderOperationFilter>();

    var apiXml = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXml);
    if (File.Exists(apiXmlPath))
    {
        c.IncludeXmlComments(apiXmlPath);
    }

    var appXml = $"{typeof(PaymentService.Application.DTOs.Payments.PaymentDto).Assembly.GetName().Name}.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXml);
    if (File.Exists(appXmlPath))
    {
        c.IncludeXmlComments(appXmlPath);
    }
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<PaymentOutboxProcessor>();

var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "SuperSecretKeyForDevelopmentOnly1234567890!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PaymentServiceDbContext>("Database");

var app = builder.Build();

app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
