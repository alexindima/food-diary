using FluentValidation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Users;

public static class DependencyInjection {
    public static IServiceCollection AddUsersModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IUserAdministrationMutationService, UserAdministrationMutationService>();
        services.AddScoped<IUserAdministrationReadService, UserAdministrationReadService>();
        services.AddScoped<IUserIdentityMutationService, UserIdentityMutationService>();
        services.AddScoped<IUserAuthenticationIdentityService, UserAuthenticationIdentityService>();
        services.AddScoped<IUserAuthenticationRegistrationService, UserAuthenticationRegistrationService>();
        services.AddScoped<IUserCredentialVerificationService, UserCredentialVerificationService>();
        services.AddScoped<IUserBillingService, UserBillingService>();
        services.AddScoped<IUserNotificationProfileService, UserNotificationProfileService>();
        services.AddScoped<UserContextService>();
        services.AddScoped<ICurrentUserAccessService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserContextService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserAiProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserDashboardProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserDietologistProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserGamificationProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserHydrationProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserTdeeProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserWeeklyCheckInProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IProfileOverviewReadService, ProfileOverviewReadService>();

        return services;
    }
}
