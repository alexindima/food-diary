using FoodDiary.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Interceptors;

internal sealed class DomainEventDispatchInterceptor(
    ILogger<DomainEventDispatchInterceptor> logger) : SaveChangesInterceptor {
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) {
        if (eventData.Context is not null) {
            DispatchDomainEvents(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void DispatchDomainEvents(DbContext context) {
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
                "Domain event dispatched: {EventType} at {OccurredOnUtc}",
                domainEvent.GetType().Name,
                domainEvent.OccurredOnUtc.ToString("O"));
        }
    }
}
