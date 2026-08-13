using FluentValidation;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Gamification;

public static class DependencyInjection {
    public static IServiceCollection AddGamificationModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IGamificationReadService, GamificationReadService>();
        services.AddScoped<IGamificationUserProfileService, GamificationUserProfileService>();
        services.AddScoped<IAchievementAwardService, AchievementAwardService>();
        services.AddScoped<IAchievementReconciliationHandler, AchievementReconciliationHandler>();
        services.AddScoped<IAchievementDefinitionAdministrationService, AchievementDefinitionAdministrationService>();
        return services;
    }
}
