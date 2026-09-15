using FoodDiary.Infrastructure;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Wearables.Infrastructure.IntegrationTests;

[ExcludeFromCodeCoverage]
internal static class WearableTestComposition {
    public static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddWearablesModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
