using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Cycles.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class CyclesModuleRegistrationTests {
    [Fact]
    public void AddCyclesModule_RegistersRepositoryAndAllNarrowAliases() {
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
        services.AddCyclesModule();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        ICycleRepository repository = scope.ServiceProvider.GetRequiredService<ICycleRepository>();

        Assert.Multiple(
            () => Assert.IsType<CycleRepository>(repository),
            () => Assert.Same(repository, scope.ServiceProvider.GetRequiredService<ICycleReadRepository>()),
            () => Assert.Same(repository, scope.ServiceProvider.GetRequiredService<ICycleReadModelRepository>()),
            () => Assert.Same(repository, scope.ServiceProvider.GetRequiredService<ICycleWriteRepository>()));
    }
}
