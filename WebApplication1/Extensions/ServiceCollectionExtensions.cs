using WebApplication1.Features.Auth.Services;

namespace WebApplication1.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddLoginSecurity(this IServiceCollection services)
    {
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IEmailService, SendGridEmailService>();
        services.AddHttpClient<CaptchaService>();
            
        return services;
    }
}