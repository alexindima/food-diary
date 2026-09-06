using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Admin;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Admin;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class AdminModuleRegistration {
    public static IServiceCollection AddAdminModule(this IServiceCollection services) =>
        services.AddAdminApplication().AddAdminPersistence();

    public static IServiceCollection AddAdminPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, AdminUserDataPurgeParticipant>());
        services.AddScoped<IAdminUserRoleAuditRepository, AdminUserRoleAuditRepository>();
        services.AddScoped<IAdminUserRoleAuditReadRepository>(static provider => provider.GetRequiredService<IAdminUserRoleAuditRepository>());
        services.AddScoped<IAdminBillingRepository, AdminBillingRepository>();
        services.AddScoped<IAdminBillingReadRepository>(static provider => provider.GetRequiredService<IAdminBillingRepository>());
        services.AddScoped<IAdminImpersonationSessionRepository, AdminImpersonationSessionRepository>();
        services.AddScoped<IAdminImpersonationSessionReadRepository>(static provider => provider.GetRequiredService<IAdminImpersonationSessionRepository>());
        services.AddScoped<IAdminImpersonationSessionWriteRepository>(static provider => provider.GetRequiredService<IAdminImpersonationSessionRepository>());
        services.AddSingleton<IAdminImpersonationHandoffService, AdminImpersonationHandoffService>();
        return services;
    }
}
