using FoodDiary.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Shared;

internal static class ModuleContextSaveCoordinator {
    public static async Task SaveAsync(FoodDiaryDbContext context, ILogger logger, CancellationToken cancellationToken = default) {
        DbContext[] participants = [context, .. context.ModuleContexts];
        if (!context.Database.IsRelational()) {
            if (participants.Count(participant => participant.ChangeTracker.HasChanges()) > 1) {
                throw new InvalidOperationException("Atomic saves across contexts require a relational provider.");
            }
            await SaveParticipantsAsync(context, participants, cancellationToken).ConfigureAwait(false);
            AcceptChanges(participants);
            return;
        }

        if (context.Database.CurrentTransaction is not null) {
            await SaveInTransactionAsync(context, participants, context.Database.CurrentTransaction, logger, cancellationToken).ConfigureAwait(false);
        } else {
            await context.Database.CreateExecutionStrategy().ExecuteAsync(async token => {
                IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(token).ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false)) {
                    await SaveInTransactionAsync(context, participants, transaction, logger, token).ConfigureAwait(false);
                    await transaction.CommitAsync(token).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        AcceptChanges(participants);
    }

    private static async Task SaveInTransactionAsync(
        FoodDiaryDbContext context,
        DbContext[] participants,
        IDbContextTransaction transaction,
        ILogger logger,
        CancellationToken cancellationToken) {
        const string savepoint = "module_unit_of_work";
        await transaction.CreateSavepointAsync(savepoint, cancellationToken).ConfigureAwait(false);
        try {
            foreach (DbContext module in participants.Skip(1)) {
                await module.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
            }
            await SaveParticipantsAsync(context, participants, cancellationToken).ConfigureAwait(false);
            await transaction.ReleaseSavepointAsync(savepoint, cancellationToken).ConfigureAwait(false);
        } catch {
            try {
                await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None).ConfigureAwait(false);
            } catch (Exception rollbackException) {
                logger.LogWarning(rollbackException, "Could not roll back module savepoint; preserving the original persistence failure.");
            }
            throw;
        } finally {
            foreach (DbContext module in participants.Skip(1)) {
                await module.Database.UseTransactionAsync(transaction: null, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private static async Task SaveParticipantsAsync(FoodDiaryDbContext context, DbContext[] participants, CancellationToken cancellationToken) {
        context.IsCoordinatingModuleSave = true;
        try {
            foreach (DbContext participant in participants) {
                if (ReferenceEquals(participant, context) || participant.ChangeTracker.HasChanges()) {
                    await participant.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken).ConfigureAwait(false);
                }
            }
        } finally {
            context.IsCoordinatingModuleSave = false;
        }
    }

    private static void AcceptChanges(DbContext[] participants) {
        foreach (DbContext participant in participants) {
            participant.ChangeTracker.AcceptAllChanges();
            DomainEventDispatcher.ClearDomainEvents(participant);
        }
    }
}
