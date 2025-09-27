using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApplication1.Extensions;
using WebApplication1.Features.Auth.Services;
using WebApplication1.Infrastructure.Configuration;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Middlewares;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLoginSecurity();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRateLimiter(options =>
{
        // Global limiter: 20 requests per minute per IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

        // Limiter for AuthController endpoints: 5 requests per minute per user
    options.AddPolicy("AuthPolicy", context =>
    {
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
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
        }
        return RateLimitPartition.GetNoLimiter("NoAuth");
    });
});
var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? throw new InvalidOperationException("Frontend:Origin is missing in configuration.");
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
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter your token in the text input below.\r\n\r\n" +
                      "Example: \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });
});
builder.Services.AddControllers();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
var sendGridKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
if (string.IsNullOrEmpty(sendGridKey))
    throw new InvalidOperationException("SENDGRID_API_KEY is missing from environment.");

var emailSettingsSection = builder.Configuration.GetSection("SendGrid");
builder.Services.Configure<EmailSettings>(options =>
{
    options.ApiKey = sendGridKey;
    options.SenderEmail = emailSettingsSection["SenderEmail"] ?? throw new InvalidOperationException();
    options.SenderName = emailSettingsSection["SenderName"] ?? throw new InvalidOperationException();
});

builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddEndpoints();
var conf = builder.Configuration["Auth:Key"];
if (conf == null) throw new Exception("Auth:Key is not set in configuration.");

var key = Encoding.UTF8.GetBytes(conf);
const string RefreshScheme = "RefreshScheme";
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt =>
{
    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT OnMessageReceived event triggered");

            var accessToken = context.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
                logger.LogDebug("Access token retrieved from cookie");
            }
            else
            {
                logger.LogDebug("No access token found in cookie");
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("JWT token successfully validated for user: {UserId}",
                context.Principal?.Identity?.Name ?? "Unknown");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Error}", context.Exception?.Message);
            return Task.CompletedTask;
        }
    };
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
}).AddJwtBearer(RefreshScheme, opt =>
{
    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("OnMessageReceived event triggered");
            var accessToken = ctx.Request.Cookies["access_token"];
            logger.LogDebug("Access token: {AccessToken}", accessToken);
            if (!string.IsNullOrEmpty(accessToken)) ctx.Token = accessToken;

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("OnTokenValidated event triggered");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("OnAuthenticationFailed event triggered");
            return Task.CompletedTask;
        }
    };

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = false,
        IssuerSigningKey =  new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,           
        ClockSkew = TimeSpan.Zero
    }; 
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RefreshTokenPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(RefreshScheme).RequireAuthenticatedUser();
    });
});
builder.Services.AddOpenApi();
var app = builder.Build();
app.MapSwagger();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapEndpoints();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.Run();