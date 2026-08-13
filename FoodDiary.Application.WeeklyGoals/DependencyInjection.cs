using FluentValidation;
using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.WeeklyGoals;

public static class DependencyInjection {
    public static IServiceCollection AddWeeklyGoalsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<WeeklyGoalProgressReader>();
        services.AddScoped<IWeeklyGoalReadService, WeeklyGoalReadService>();
        services.AddScoped<WeeklyGoalReminderProcessor>();
        return services;
    }
}
