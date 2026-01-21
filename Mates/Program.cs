using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mates.Features.Auth.Services;
using Mates.Infrastructure.Configuration;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities;
using Mates.Infrastructure.Service;
using Mates.Shared.Endpoints;
using Mates.Shared.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? throw new InvalidOperationException(
                           "DATABASE_CONNECTION_STRING is missing from environment.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IEmailService, PostmarkEmailService>();
builder.Services.AddScoped<ISettlementCalculator, SettlementCalculatorService>();
builder.Services.AddLoginSecurity();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 1
            }));

    options.AddPolicy("AuthPolicy", context =>
    {
        if (!context.Request.Path.StartsWithSegments("/api/auth"))
            return RateLimitPartition.GetNoLimiter("NoAuth");

        var user = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(
            user,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var frontendOrigin = Environment.GetEnvironmentVariable("FRONTEND_ORIGIN")
                     ?? throw new InvalidOperationException("FRONTEND_ORIGIN is missing from environment.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigin)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var postmarkApiKey = Environment.GetEnvironmentVariable("POSTMARK_API_KEY");
if (string.IsNullOrEmpty(postmarkApiKey))
    throw new InvalidOperationException("POSTMARK_API_KEY is missing from environment.");

var emailSettingsSection = builder.Configuration.GetSection("Postmark");
builder.Services.Configure<EmailSettings>(options =>
{
    options.ApiKey = postmarkApiKey;
    options.SenderEmail = emailSettingsSection["SenderEmail"] ?? throw new InvalidOperationException();
    options.SenderName = emailSettingsSection["SenderName"] ?? throw new InvalidOperationException();
});

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
var secret = Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrWhiteSpace(issuer))
    throw new InvalidOperationException("JWT Issuer is missing.");
if (string.IsNullOrWhiteSpace(audience))
    throw new InvalidOperationException("JWT Audience is missing.");
if(string.IsNullOrWhiteSpace(secret))
    throw new InvalidOperationException("JWT SecretKey is missing.");
if (string.IsNullOrWhiteSpace(secret))
    throw new InvalidOperationException("JWT SecretKey is missing.");
if (secret.Length < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long for security.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

builder.Services.AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API with JWT Authentication"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token below."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});
builder.Services.AddEndpoints();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var uploadsPath = Environment.GetEnvironmentVariable("UPLOADS_PATH") 
                  ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        if (!(!ctx.Context.User.Identity?.IsAuthenticated ?? true)) return;
        ctx.Context.Response.StatusCode = 401;
        ctx.Context.Response.ContentLength = 0;
        ctx.Context.Response.Body = Stream.Null;
    }
});
app.MapControllers();
app.MapEndpoints();
app.Run();