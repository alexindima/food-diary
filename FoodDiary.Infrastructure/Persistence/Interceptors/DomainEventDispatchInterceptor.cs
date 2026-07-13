using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Interceptors;

internal sealed class DomainEventDispatchInterceptor(
    IDomainEventPublisher publisher,
    ILogger<DomainEventDispatchInterceptor> logger) : SaveChangesInterceptor {
    // Domain event handlers run inside SaveChanges so they can add transactional state.
    // Critical external side effects must write durable outbox state; post-commit actions are best-effort only.
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) {
        if (eventData.Context is not null) {
            await DomainEventDispatcher.DispatchAsync(eventData.Context, publisher, logger, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }
}
