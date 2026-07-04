using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Infrastructure.Persistence.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddDashboardReadServices(this IServiceCollection services) {
        services.RemoveAll<IDashboardStatisticsReadService>();
        services.RemoveAll<IDashboardBodyReadService>();
        services.RemoveAll<IDashboardMealsReadService>();
        services.RemoveAll<IDashboardReadService>();

        services.AddScoped<DashboardStatisticsReadService>();
        services.AddScoped<IDashboardStatisticsReadService>(static provider => provider.GetRequiredService<DashboardStatisticsReadService>());
        services.AddScoped<DashboardBodyReadService>();
        services.AddScoped<IDashboardBodyReadService>(static provider => provider.GetRequiredService<DashboardBodyReadService>());
        services.AddScoped<DashboardMealsReadService>();
        services.AddScoped<IDashboardMealsReadService>(static provider => provider.GetRequiredService<DashboardMealsReadService>());
        services.AddScoped<IDashboardReadService, DashboardReadService>();

        return services;
    }
}
