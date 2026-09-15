using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;
using FoodDiary.Modules.DailyAdvices.Application;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddDailyAdvicesModule(this IServiceCollection services) {
        services.AddDailyAdvicesApplication();
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<DailyAdvicesDbContext>(static options => new DailyAdvicesDbContext(options)));
        services.AddScoped<IDailyAdviceReadModelRepository>(static provider => new DailyAdviceRepository(
            provider.GetRequiredService<DailyAdvicesDbContext>().DailyAdvices));
        return services;
    }
}
