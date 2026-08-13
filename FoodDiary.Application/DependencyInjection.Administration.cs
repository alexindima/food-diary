using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddAdministrationModules(this IServiceCollection services) {
        services.AddScoped<IAdminAiUsageReadService, AdminAiUsageReadService>();
        services.AddScoped<IAdminAuditReadService, AdminAuditReadService>();
        services.AddScoped<IAdminBillingReadService, AdminBillingReadService>();
        services.AddScoped<IAdminContentReadService, AdminContentReadService>();
        services.AddScoped<IAdminDashboardReadService, AdminDashboardReadService>();
        services.AddScoped<IAdminUserReadService, AdminUserReadService>();
        services.AddScoped<IAdminUserLoginReadService, AdminUserLoginReadService>();
    }
}
