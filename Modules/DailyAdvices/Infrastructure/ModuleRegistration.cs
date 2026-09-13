using FoodDiary.Application.Abstractions.DailyAdvices.Common;
using FoodDiary.Application.DailyAdvices;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.DailyAdvices.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddDailyAdvicesModule(this IServiceCollection services) {
        services.AddDailyAdvicesApplication();
        services.AddScoped(static provider => provider.GetRequiredService<FoodDiaryDbContext>()
            .CreateModuleContext<DailyAdvicesDbContext>(static options => new DailyAdvicesDbContext(options)));
        services.AddScoped<IDailyAdviceReadModelRepository>(static provider => new DailyAdviceRepository(
            provider.GetRequiredService<DailyAdvicesDbContext>().DailyAdvices));
        return services;
    }
}
