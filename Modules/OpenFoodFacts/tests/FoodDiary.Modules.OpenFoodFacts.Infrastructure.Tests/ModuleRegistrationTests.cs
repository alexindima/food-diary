using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddOpenFoodFactsModule_RegistersCacheRepositoryPorts() {
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddSingleton(TimeProvider.System);

        services.AddOpenFoodFactsModule();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IOpenFoodFactsProductCacheRepository repository = scope.ServiceProvider.GetRequiredService<IOpenFoodFactsProductCacheRepository>();

        Assert.Multiple(
            () => Assert.Same(repository, scope.ServiceProvider.GetRequiredService<IOpenFoodFactsProductCacheReadRepository>()),
            () => Assert.Same(repository, scope.ServiceProvider.GetRequiredService<IOpenFoodFactsProductCacheWriteRepository>()));
    }
}
