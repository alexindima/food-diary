using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Wearables.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class ModuleRegistrationTests {
    [Fact]
    public void AddWearablesModule_ResolvesAllRepositoryAliasesToTheirOwnedScopedRepository() {
        var services = new ServiceCollection();
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
