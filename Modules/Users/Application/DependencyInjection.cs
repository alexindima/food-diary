using FluentValidation;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Users.Application;

public static class DependencyInjection {
    public static IServiceCollection AddUsersApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IUserIdentityMutationService, UserIdentityMutationService>();
        services.AddScoped<IUserAuthenticationIdentityService, UserAuthenticationIdentityService>();
        services.AddScoped<IUserAuthenticationRegistrationService, UserAuthenticationRegistrationService>();
        services.AddScoped<IUserTelegramAccountService, UserTelegramAccountService>();
        services.AddScoped<IUserCredentialVerificationService, UserCredentialVerificationService>();
        services.AddScoped<IUserNotificationProfileService, UserNotificationProfileService>();
        services.AddScoped<UserContextService>();
        services.AddScoped<IUserContextService>(static provider => provider.GetRequiredService<UserContextService>());
        services.AddScoped<IUserProfileReadService>(static provider => provider.GetRequiredService<UserContextService>());

        return services;
    }
}
