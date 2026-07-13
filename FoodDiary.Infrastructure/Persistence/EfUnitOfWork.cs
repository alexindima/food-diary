using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(
    FoodDiaryDbContext context,
    IDomainEventPublisher domainEventPublisher,
    ILogger<EfUnitOfWork> logger) : IUnitOfWork {
    public bool HasPendingChanges => context.ChangeTracker.HasChanges();

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        await DomainEventDispatcher.DispatchAsync(
            context,
            domainEventPublisher,
            logger,
            cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
