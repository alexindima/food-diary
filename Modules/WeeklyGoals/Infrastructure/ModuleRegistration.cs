using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals;
using FoodDiary.Infrastructure.Persistence.WeeklyGoals;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddWeeklyGoalsModule(this IServiceCollection services) {
        services.AddWeeklyGoalsApplication();
        services.AddScoped<IWeeklyGoalRepository, WeeklyGoalRepository>();
        services.AddScoped<IWeeklyGoalTransactionRunner, EfWeeklyGoalTransactionRunner>();
        return services;
    }
}
