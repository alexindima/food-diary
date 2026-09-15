using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Persistence.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration) {
        services.AddPersistenceRuntime(configuration);
        services.AddScoped<FoodDiaryDbContext>(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<FoodDiaryDbContext>(static options => new FoodDiaryDbContext(options), saveOrder: 1));
        services.AddScoped<ICompositionReadContext>(static provider => provider.GetRequiredService<FoodDiaryDbContext>());
    }
}
