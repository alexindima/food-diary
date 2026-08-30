using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Gamification;
using FoodDiary.Modules.Gamification.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Gamification.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddGamificationModule(this IServiceCollection services) {
        services.AddGamificationApplication();
        services.AddScoped<IAchievementDefinitionStore, AchievementDefinitionStore>();
        services.AddScoped<IUserAchievementStore, UserAchievementStore>();
        services.AddScoped<IAchievementEvaluationOutbox, AchievementEvaluationOutbox>();
        services.AddScoped<IAchievementMetricReader, AchievementMetricReader>();
        services.AddScoped<IAchievementEvaluationOutboxProcessor, AchievementEvaluationOutboxProcessor>();
        return services;
    }
}
