using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Wearables.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
internal static class WearableTestComposition {
    public static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddWearablesModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
