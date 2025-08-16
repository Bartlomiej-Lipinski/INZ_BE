using WebApplication1.Features.Auth;

namespace WebApplication1.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLoginSecurity(this IServiceCollection services)
    {
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddHttpClient<CaptchaService>();
            
        return services;
    }
}