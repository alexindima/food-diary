using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Interceptors;

internal static class DomainEventDispatcher {
    public static async Task DispatchAsync(
        DbContext context,
        IDomainEventPublisher publisher,
        ILogger logger,
        CancellationToken cancellationToken) {
        while (true) {
            IAggregateWithEvents[] aggregates = [.. context.ChangeTracker
                .Entries<IAggregateWithEvents>()
                .Where(static entry => entry.Entity.DomainEvents.Count > 0)
                .Select(static entry => entry.Entity)];
            IDomainEvent[] events = [.. aggregates.SelectMany(static aggregate => aggregate.DomainEvents)];
            if (events.Length == 0) {
                return;
            }

            foreach (IAggregateWithEvents aggregate in aggregates) {
                aggregate.ClearDomainEvents();
            }

            foreach (IDomainEvent domainEvent in events) {
                logger.LogInformation(
                    "Dispatching domain event: {EventType} at {OccurredOnUtc}",
                    domainEvent.GetType().Name,
                    domainEvent.OccurredOnUtc.ToString("O"));
                await publisher.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
