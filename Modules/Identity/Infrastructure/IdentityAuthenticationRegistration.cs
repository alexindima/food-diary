using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Services;
using FoodDiary.Modules.Identity.Contracts.Authentication.Services;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Infrastructure;

public static class IdentityAuthenticationRegistration {
    public static IServiceCollection AddIdentityAuthenticationInfrastructure(this IServiceCollection services) {
        services.AddOptions<PasswordLoginThrottleOptions>().BindConfiguration(PasswordLoginThrottleOptions.SectionName)
            .Validate(PasswordLoginThrottleOptions.IsValid, "Password login throttle limits are invalid.").ValidateOnStart();
        services.AddSingleton<IPasswordLoginThrottle, InMemoryPasswordLoginThrottle>();
        services.AddOptions<BrowserSessionRenewalOptions>().BindConfiguration(BrowserSessionRenewalOptions.SectionName)
            .Validate(BrowserSessionRenewalOptions.IsValid, "Browser session renewal limits are invalid.").ValidateOnStart();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IAuthenticationSessionLifetimePolicy, AuthenticationSessionLifetimePolicy>();
        services.AddSingleton<IImpersonationTokenIssuer, ImpersonationTokenIssuer>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        return services;
    }
}
