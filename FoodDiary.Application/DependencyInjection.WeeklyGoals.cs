using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddWeeklyGoalModule(this IServiceCollection services) {
        services.AddScoped<WeeklyGoalProgressReader>();
        services.AddScoped<IWeeklyGoalReadService, WeeklyGoalReadService>();
        services.AddScoped<WeeklyGoalReminderProcessor>();
    }
}
