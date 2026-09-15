using FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FoodDiary.Modules.OpenFoodFacts.Infrastructure.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddOpenFoodFactsModule_RegistersCacheRepositoryPorts() {
        var services = new ServiceCollection();
        IModuleContextFactory contexts = Substitute.For<IModuleContextFactory>();
        contexts.CreateModuleContext(Arg.Any<Func<DbContextOptions<OpenFoodFactsDbContext>, OpenFoodFactsDbContext>>(), Arg.Any<int>())
            .Returns(call => call.Arg<Func<DbContextOptions<OpenFoodFactsDbContext>, OpenFoodFactsDbContext>>()(
                new DbContextOptionsBuilder<OpenFoodFactsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options));
        services.AddSingleton(contexts);
        services.AddScoped(_ => Substitute.For<IModuleTransactionCoordinator>());
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
