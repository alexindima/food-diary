using FluentValidation;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Common;
using FoodDiary.Modules.Dashboard.Application.Common;
using FoodDiary.Modules.Dashboard.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Dashboard.Application;

public static class DependencyInjection {
    public static IServiceCollection AddDashboardModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.TryAddScoped<IDashboardBodyReadService, RepositoryDashboardBodyReadService>();
        services.TryAddScoped<IDashboardMealsReadService, MediatorDashboardMealsReadService>();
        services.TryAddScoped<IDashboardReadService, ComposedDashboardReadService>();
        services.AddScoped<IDashboardUserContextService, DashboardUserContextService>();
        services.AddScoped<IDashboardSectionDataLoader, DashboardSectionDataLoader>();
        services.AddScoped<IDashboardSnapshotBuilder>(static serviceProvider =>
            new DashboardSnapshotBuilder(
                serviceProvider.GetRequiredService<IDashboardSectionDataLoader>(),
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DashboardSnapshotBuilder>>()));
        return services;
    }
}
