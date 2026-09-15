using FluentValidation;
using FoodDiary.Modules.WeeklyGoals.Application.Common;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.WeeklyGoals.Application;

public static class DependencyInjection {
    public static IServiceCollection AddWeeklyGoalsApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<WeeklyGoalProgressReader>();
        return services;
    }
}
