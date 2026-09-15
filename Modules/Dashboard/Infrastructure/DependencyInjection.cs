using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Dashboard.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddDashboardReadServices(this IServiceCollection services) {
        services.RemoveAll<IDashboardStatisticsReadService>();

        services.AddScoped<DashboardStatisticsReadService>();
        services.AddScoped<IDashboardStatisticsReadService>(static provider => provider.GetRequiredService<DashboardStatisticsReadService>());
        return services;
    }
}
