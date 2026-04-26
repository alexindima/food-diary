using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Interceptors;

internal sealed class DomainEventDispatchInterceptor(
    IDomainEventPublisher publisher,
    ILogger<DomainEventDispatchInterceptor> logger) : SaveChangesInterceptor {
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) {
        if (eventData.Context is not null) {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken) {
        var aggregates = context.ChangeTracker
            .Entries<IAggregateWithEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates) {
            aggregate.ClearDomainEvents();
        }

        foreach (var domainEvent in events) {
            logger.LogInformation(
                "Dispatching domain event: {EventType} at {OccurredOnUtc}",
                domainEvent.GetType().Name,
                domainEvent.OccurredOnUtc.ToString("O"));

            await publisher.PublishAsync(domainEvent, cancellationToken);
        }
    }
}
