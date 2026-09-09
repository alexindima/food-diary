using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Marketing.Infrastructure;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingModuleRegistrationTests {
    [Fact]
    public async Task AddMarketingModule_RepositoryAliasesResolveThroughSameScopedInstance() {
        var services = new ServiceCollection();
        services.AddDbContext<FoodDiaryDbContext>(options => options.UseNpgsql("Host=localhost;Database=registration"));
        services.AddMarketingModule();
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope firstScope = provider.CreateAsyncScope();
        await using AsyncServiceScope secondScope = provider.CreateAsyncScope();

        MarketingAttributionEventRepository repository = firstScope.ServiceProvider.GetRequiredService<MarketingAttributionEventRepository>();
        Assert.Multiple(
            () => Assert.Same(repository, firstScope.ServiceProvider.GetRequiredService<IMarketingAttributionEventRepository>()),
            () => Assert.Same(repository, firstScope.ServiceProvider.GetRequiredService<IMarketingAttributionRangeReadRepository>()),
            () => Assert.Same(repository, firstScope.ServiceProvider.GetRequiredService<IMarketingAttributionEventReadRepository>()),
            () => Assert.Same(repository, firstScope.ServiceProvider.GetRequiredService<IMarketingAttributionEventWriteRepository>()),
            () => Assert.NotSame(repository, secondScope.ServiceProvider.GetRequiredService<IMarketingAttributionEventRepository>()));
    }
}
