using FluentValidation;
using FoodDiary.Modules.Admin.Application.Common;
using FoodDiary.Modules.Admin.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Admin.Application;

public static class DependencyInjection {
    public static IServiceCollection AddAdminApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IAdminDashboardReadService, AdminDashboardReadService>();
        return services;
    }
}
