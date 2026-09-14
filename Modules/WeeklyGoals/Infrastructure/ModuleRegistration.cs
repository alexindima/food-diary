using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals;
using FoodDiary.Infrastructure.Persistence.WeeklyGoals;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddWeeklyGoalsModule(this IServiceCollection services) {
        services.AddWeeklyGoalsApplication();
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<WeeklyGoalsDbContext>(options => new WeeklyGoalsDbContext(options)));
        services.AddScoped<IWeeklyGoalRepository>(provider => {
            IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
            return new WeeklyGoalRepository(provider.GetRequiredService<WeeklyGoalsDbContext>(),
                () => coordinator.CurrentTransaction);
        });
        services.AddScoped<IWeeklyGoalTransactionRunner, EfWeeklyGoalTransactionRunner>();
        return services;
    }
}
