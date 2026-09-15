using FoodDiary.Persistence.Runtime.Persistence.Interceptors;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Runtime.Persistence;

internal sealed class EfUnitOfWork(
    SharedPersistenceDbContext context,
    IDomainEventPublisher domainEventPublisher,
    ILogger<EfUnitOfWork> logger) : IUnitOfWork {
    public bool HasPendingChanges => context.ChangeTracker.HasChanges()
        || context.ModuleContexts.Any(module => module.ChangeTracker.HasChanges());

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        await DomainEventDispatcher.DispatchAsync(
            context,
            domainEventPublisher,
            logger,
            cancellationToken).ConfigureAwait(false);
        for (int index = 0; index < context.ModuleContexts.Count; index++) {
            DbContext module = context.ModuleContexts[index];
            await DomainEventDispatcher.DispatchAsync(module, domainEventPublisher, logger, cancellationToken).ConfigureAwait(false);
        }
        if (context.ModuleContexts.Any(module => module.ChangeTracker.HasChanges())) {
            await ModuleContextSaveCoordinator.SaveAsync(context.Session, logger, cancellationToken).ConfigureAwait(false);
            return;
        }
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        DomainEventDispatcher.ClearDomainEvents(context);
    }
}
