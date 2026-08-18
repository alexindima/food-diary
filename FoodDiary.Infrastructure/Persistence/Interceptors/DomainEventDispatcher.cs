using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace FoodDiary.Infrastructure.Persistence.Interceptors;

internal static class DomainEventDispatcher {
    private static readonly ConditionalWeakTable<DbContext, HashSet<IDomainEvent>> PublishedEvents = [];

    public static async Task DispatchAsync(
        DbContext context,
        IDomainEventPublisher publisher,
        ILogger logger,
        CancellationToken cancellationToken) {
        HashSet<IDomainEvent> publishedEvents = PublishedEvents.GetValue(
            context,
            static _ => new HashSet<IDomainEvent>(ReferenceEqualityComparer.Instance));
        while (true) {
            IDomainEvent[] events = [
                .. context.ChangeTracker
                    .Entries<IAggregateWithEvents>()
                    .SelectMany(static entry => entry.Entity.DomainEvents)
                    .Where(domainEvent => !publishedEvents.Contains(domainEvent)),
            ];
            if (events.Length == 0) {
                return;
            }

            foreach (IDomainEvent domainEvent in events) {
                publishedEvents.Add(domainEvent);
                logger.LogInformation(
                    "Dispatching domain event: {EventType} at {OccurredOnUtc}",
                    domainEvent.EventType,
                    domainEvent.OccurredOnUtc.ToString("O"));
                await publisher.PublishAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public static void ClearDomainEvents(DbContext context) {
        foreach (IAggregateWithEvents aggregate in context.ChangeTracker
                     .Entries<IAggregateWithEvents>()
                     .Select(static entry => entry.Entity)) {
            aggregate.ClearDomainEvents();
        }

        PublishedEvents.Remove(context);
    }
}
