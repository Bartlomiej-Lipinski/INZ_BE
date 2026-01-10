using Mates.Features.Auth.Services;

namespace Mates.Shared.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddLoginSecurity(this IServiceCollection services)
    {
        services.AddScoped<ILoginAttemptService, LoginAttemptService>();
        services.AddScoped<ICaptchaService, CaptchaService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddHttpClient<CaptchaService>();

        return services;
    }
}