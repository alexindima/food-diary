using FoodDiary.Modules.Admin.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Admin.Application;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Infrastructure.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Admin.Infrastructure;

public static class AdminModuleRegistration {
    public static IServiceCollection AddAdminModule(this IServiceCollection services) =>
        services.AddAdminApplication().AddAdminPersistence();

    public static IServiceCollection AddAdminPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, AdminUserDataPurgeParticipant>());
        services.AddScoped(static provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<AdminDbContext>(static options => new AdminDbContext(options)));
        services.AddScoped<IAdminImpersonationSessionRepository>(static provider => new AdminImpersonationSessionRepository(
            provider.GetRequiredService<AdminDbContext>().AdminImpersonationSessions,
            provider.GetRequiredService<IAdminImpersonationSessionQuery>()));
        services.AddScoped<IAdminImpersonationSessionReadRepository>(static provider => provider.GetRequiredService<IAdminImpersonationSessionRepository>());
        services.AddScoped<IAdminImpersonationSessionWriteRepository>(static provider => provider.GetRequiredService<IAdminImpersonationSessionRepository>());
        services.AddSingleton<IAdminImpersonationHandoffService, AdminImpersonationHandoffService>();
        return services;
    }
}
