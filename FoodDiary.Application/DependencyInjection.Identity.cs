using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Authentication.Services;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddIdentityModules(this IServiceCollection services) {
        services.AddScoped<IAuthenticationUserLookupService, AuthenticationUserLookupService>();
        services.AddScoped<IAuthenticationLoginEventCleanupService, AuthenticationLoginEventCleanupService>();
        services.AddScoped<IAuthenticationUserMutationService, AuthenticationUserMutationService>();
        services.AddScoped<IAuthenticationUserRegistrationService, AuthenticationUserRegistrationService>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IUserIdentityMutationService, UserIdentityMutationService>();
        services.AddScoped<IDietologistClientReadService, DietologistClientReadService>();
        services.AddScoped<IDietologistInvitationReadService, DietologistInvitationReadService>();
        services.AddScoped<IDietologistRecommendationReadService, DietologistRecommendationReadService>();
        services.AddScoped<IDietologistUserLookupService, DietologistUserLookupService>();
        services.AddScoped<IDietologistUserContextService, DietologistUserContextService>();
        services.AddScoped<UserContextService>();
        services.AddScoped<ICurrentUserAccessService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserContextService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IProfileOverviewReadService, ProfileOverviewReadService>();
    }
}
