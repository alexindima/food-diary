using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Infrastructure.Persistence.WeeklyGoals;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddWeeklyGoalPersistence(this IServiceCollection services) {
        services.AddScoped<IWeeklyGoalRepository, WeeklyGoalRepository>();
        services.AddScoped<IWeeklyGoalTransactionRunner, EfWeeklyGoalTransactionRunner>();
    }
}
