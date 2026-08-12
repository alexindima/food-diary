using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddIdentityModules(this IServiceCollection services) {
        services.AddScoped<IAuthenticationLoginEventCleanupService, AuthenticationLoginEventCleanupService>();
        services.AddScoped<IAuthenticationLoginEventReadService, AuthenticationLoginEventReadService>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<IInitialAdminBootstrapService, InitialAdminBootstrapService>();
        services.AddScoped<IDietologistClientReadService, DietologistClientReadService>();
        services.AddScoped<IDietologistDashboardAccessService, DietologistDashboardAccessService>();
        services.AddScoped<IDietologistInvitationReadService, DietologistInvitationReadService>();
        services.AddScoped<IProfileDietologistReadService>(static provider =>
            (IProfileDietologistReadService)provider.GetRequiredService<IDietologistInvitationReadService>());
        services.AddScoped<IDietologistRecommendationReadService, DietologistRecommendationReadService>();
        services.AddScoped<IRecommendationDiscussionReadService, RecommendationDiscussionReadService>();
        services.AddScoped<IRecommendationTemplateReadService, RecommendationTemplateReadService>();
        services.AddScoped<IDietologistUserLookupService, DietologistUserLookupService>();
        services.AddScoped<IDietologistUserContextService, DietologistUserContextService>();
    }
}
