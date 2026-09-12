using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Modules.Dashboard.Infrastructure.Persistence.Dashboard;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Dashboard.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddDashboardReadServices(this IServiceCollection services) {
        services.RemoveAll<IDashboardStatisticsReadService>();
        services.RemoveAll<IDashboardBodyReadService>();
        services.RemoveAll<IDashboardReadService>();

        services.AddScoped<DashboardStatisticsReadService>();
        services.AddScoped<IDashboardStatisticsReadService>(static provider => provider.GetRequiredService<DashboardStatisticsReadService>());
        services.AddScoped<DashboardBodyReadService>();
        services.AddScoped<IDashboardBodyReadService>(static provider => provider.GetRequiredService<DashboardBodyReadService>());
        services.AddScoped<IDashboardReadService, DashboardReadService>();
        return services;
    }
}
