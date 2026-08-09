using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Consumptions.Common;
using FoodDiary.Infrastructure.Persistence.Achievements;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddAchievementPersistence(this IServiceCollection services) {
        services.AddScoped<IAchievementDefinitionStore, AchievementDefinitionStore>();
        services.AddScoped<IUserAchievementStore, UserAchievementStore>();
        services.AddScoped<IAchievementEvaluationOutbox, AchievementEvaluationOutbox>();
        services.AddScoped<IAchievementEvaluationOutboxProcessor, AchievementEvaluationOutboxProcessor>();
    }
}
