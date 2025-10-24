using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApplication1.Extensions;
using WebApplication1.Features.Auth.Services;
using WebApplication1.Infrastructure.Configuration;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Storage;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Middlewares;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddLoginSecurity();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    // Global limiter: 20 requests/min/IP
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

    // Auth limiter: 5 requests/min/user
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

var frontendOrigin = builder.Configuration["Frontend:Origin"]
                     ?? throw new InvalidOperationException("Frontend:Origin is missing in configuration.");

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

builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var jwtSection = builder.Configuration.GetSection("JwtSettings");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
var secret = jwtSection["SecretKey"];

if (builder.Environment.IsProduction())
{
    issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? issuer;
    audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? audience;
    secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? secret;
}

if (string.IsNullOrWhiteSpace(secret))
    throw new InvalidOperationException("JWT SecretKey is missing.");
if (secret.Length < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long for security.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
const string RefreshScheme = "RefreshScheme";

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
        ValidateIssuer = !builder.Environment.IsDevelopment(),
        ValidateAudience = !builder.Environment.IsDevelopment(),
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = key,
        ClockSkew = TimeSpan.Zero
    };
})
.AddJwtBearer(RefreshScheme, opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = !builder.Environment.IsDevelopment(),
        ValidateAudience = !builder.Environment.IsDevelopment(),
        ValidateLifetime = false,
        ValidIssuer = issuer,
        ValidAudience = audience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RefreshTokenPolicy", policy =>
    {
        policy.AddAuthenticationSchemes(RefreshScheme)
            .RequireAuthenticatedUser();
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
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        if (!ctx.Context.User.Identity?.IsAuthenticated ?? true)
        {
            ctx.Context.Response.StatusCode = 401;
            ctx.Context.Response.ContentLength = 0;
            ctx.Context.Response.Body = Stream.Null;
        }
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapEndpoints();
app.UseHttpsRedirection();

app.Run();
