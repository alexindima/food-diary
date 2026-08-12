using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Authentication.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddIdentityModules(this IServiceCollection services) {
        services.AddScoped<IAuthenticationLoginEventCleanupService, AuthenticationLoginEventCleanupService>();
        services.AddScoped<IAuthenticationLoginEventReadService, AuthenticationLoginEventReadService>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<IInitialAdminBootstrapService, InitialAdminBootstrapService>();
    }
}
