using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Services;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Authentication.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Infrastructure;

public static class IdentityAuthenticationRegistration {
    public static IServiceCollection AddIdentityAuthenticationInfrastructure(this IServiceCollection services) {
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IImpersonationTokenIssuer, ImpersonationTokenIssuer>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        return services;
    }
}
