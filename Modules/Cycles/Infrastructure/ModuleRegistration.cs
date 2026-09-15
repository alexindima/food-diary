using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Cycles.Application.Abstractions.Common;
using FoodDiary.Modules.Cycles.Application;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Cycles.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddCyclesModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, CyclesUserDataPurgeParticipant>());
        services.AddCyclesApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<CyclesDbContext>(static options => new CyclesDbContext(options)));
        services.AddScoped<CycleRepository>(static provider => new CycleRepository(
            provider.GetRequiredService<CyclesDbContext>().CycleProfiles));
        services.AddScoped<ICycleReadModelRepository>(static provider => provider.GetRequiredService<CycleRepository>());
        services.AddScoped<ICycleWriteRepository>(static provider => provider.GetRequiredService<CycleRepository>());
        return services;
    }
}
