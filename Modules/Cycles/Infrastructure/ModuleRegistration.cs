using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Cycles;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Cycles.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddCyclesModule(this IServiceCollection services) {
        services.AddCyclesApplication();
        services.AddScoped<ICycleRepository, CycleRepository>();
        services.AddScoped<ICycleReadRepository>(static provider => provider.GetRequiredService<ICycleRepository>());
        services.AddScoped<ICycleReadModelRepository>(static provider => provider.GetRequiredService<ICycleRepository>());
        services.AddScoped<ICycleWriteRepository>(static provider => provider.GetRequiredService<ICycleRepository>());
        return services;
    }
}
