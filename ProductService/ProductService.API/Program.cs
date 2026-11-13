using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductService.Infrastructure.Data;
using ProductService.API.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ProductService.Application;
using ProductService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174"
        )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProductService API",
        Version = "v1",
        Description = "Product Service API for managing product catalog, categories, inventory, and bulk imports."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
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

    c.OperationFilter<ProductService.API.Swagger.AuthorizeCheckOperationFilter>();

    c.OperationFilter<ProductService.API.Swagger.InternalServiceHeaderOperationFilter>();
    c.OperationFilter<ProductService.API.Swagger.TotalCountHeaderOperationFilter>();

    var apiXml = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXml);
    if (File.Exists(apiXmlPath))
    {
        c.IncludeXmlComments(apiXmlPath);
    }

    var appXml = $"{typeof(ProductService.Application.DTOs.Products.ProductDto).Assembly.GetName().Name}.xml";
    var appXmlPath = Path.Combine(AppContext.BaseDirectory, appXml);
    if (File.Exists(appXmlPath))
    {
        c.IncludeXmlComments(appXmlPath);
    }
});

// Infrastructure and Application services
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.Configure<ProductService.Application.Configuration.BulkProductOptions>(builder.Configuration.GetSection("BulkProduct"));
builder.Services.AddApplicationServices();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHostedService<ProductService.API.BackgroundServices.LowStockBackgroundService>();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSection.GetValue<string>("SecretKey");
if (!string.IsNullOrEmpty(secret))
{
    var key = Encoding.UTF8.GetBytes(secret);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

    builder.Services.AddAuthorization();
}
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ProductServiceDbContext>("Database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductService API v1"));
}

app.UseExceptionHandling();

app.UseHttpsRedirection();

var env = app.Environment;
var uploadsPath = System.IO.Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
if (!System.IO.Directory.Exists(uploadsPath))
{
    System.IO.Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseCors("ReactApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
