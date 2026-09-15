using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Wearables.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddWearablesModule_ResolvesAllRepositoryAliasesToTheirOwnedScopedRepository() {
        var services = new ServiceCollection();
        services.AddScoped<FoodDiary.Persistence.Abstractions.IModuleContextFactory>(provider => provider.GetRequiredService<FoodDiaryDbContext>());
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddWearablesModule();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IWearableConnectionRepository connectionRepository = scope.ServiceProvider.GetRequiredService<IWearableConnectionRepository>();
        IWearableSyncRepository syncRepository = scope.ServiceProvider.GetRequiredService<IWearableSyncRepository>();

        Assert.Multiple(
            () => Assert.Same(connectionRepository, scope.ServiceProvider.GetRequiredService<IWearableConnectionReadRepository>()),
            () => Assert.Same(connectionRepository, scope.ServiceProvider.GetRequiredService<IWearableConnectionWriteRepository>()),
            () => Assert.Same(syncRepository, scope.ServiceProvider.GetRequiredService<IWearableSyncReadRepository>()),
            () => Assert.Same(syncRepository, scope.ServiceProvider.GetRequiredService<IWearableSyncReadModelRepository>()),
            () => Assert.Same(syncRepository, scope.ServiceProvider.GetRequiredService<IWearableSyncWriteRepository>()));
    }
}
