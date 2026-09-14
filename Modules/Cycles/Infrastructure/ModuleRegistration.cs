using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Cycles.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddCyclesModule(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, CyclesUserDataPurgeParticipant>());
        services.AddCyclesApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<CyclesDbContext>(static options => new CyclesDbContext(options)));
        services.AddScoped<ICycleRepository>(static provider => new CycleRepository(
            provider.GetRequiredService<CyclesDbContext>().CycleProfiles));
        services.AddScoped<ICycleReadRepository>(static provider => provider.GetRequiredService<ICycleRepository>());
        services.AddScoped<ICycleReadModelRepository>(static provider => provider.GetRequiredService<ICycleRepository>());
        services.AddScoped<ICycleWriteRepository>(static provider => provider.GetRequiredService<ICycleRepository>());
        return services;
    }
}
