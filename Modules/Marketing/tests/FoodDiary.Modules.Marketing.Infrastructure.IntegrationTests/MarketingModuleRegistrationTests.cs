using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Modules.Marketing.Infrastructure;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MarketingModuleRegistrationTests {
    [Fact]
    public void AddMarketingModule_RepositoryAliasesResolveThroughSameScopedInstance() {
        var services = new ServiceCollection();
        services.AddMarketingModule();
        var repository = new StubRepository();
        var provider = new FixedServiceProvider(repository);

        ServiceDescriptor primaryDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IMarketingAttributionEventRepository));
        ServiceDescriptor readDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IMarketingAttributionEventReadRepository));
        ServiceDescriptor writeDescriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IMarketingAttributionEventWriteRepository));

        Assert.Multiple(
            () => Assert.Equal(typeof(MarketingAttributionEventRepository), primaryDescriptor.ImplementationType),
            () => Assert.Same(repository, readDescriptor.ImplementationFactory!(provider)),
            () => Assert.Same(repository, writeDescriptor.ImplementationFactory!(provider)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedServiceProvider(IMarketingAttributionEventRepository repository) : IServiceProvider {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IMarketingAttributionEventRepository) ? repository : null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRepository : IMarketingAttributionEventRepository {
        public Task<MarketingAttributionSummaryRecord> GetSummaryAsync(
            DateTime sinceUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MarketingAttributionEventRecord?> GetLandingAsync(
            string anonymousId,
            string sessionId,
            DateTime sinceUtc,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> ExistsForUserAsync(
            Guid userId,
            string eventType,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddAsync(
            MarketingAttributionEventRecord record,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> DeleteOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
