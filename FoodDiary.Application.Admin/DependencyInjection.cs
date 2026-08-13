using FluentValidation;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Admin;

public static class DependencyInjection {
    public static IServiceCollection AddAdminModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IAdminAiUsageReadService, AdminAiUsageReadService>();
        services.AddScoped<IAdminAuditReadService, AdminAuditReadService>();
        services.AddScoped<IAdminBillingReadService, AdminBillingReadService>();
        services.AddScoped<IAdminContentReadService, AdminContentReadService>();
        services.AddScoped<IAdminDashboardReadService, AdminDashboardReadService>();
        services.AddScoped<IAdminUserReadService, AdminUserReadService>();
        services.AddScoped<IAdminUserLoginReadService, AdminUserLoginReadService>();
        return services;
    }
}
