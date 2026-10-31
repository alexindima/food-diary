using FoodDiary.Persistence.PostgreSql;
using System.Runtime.ExceptionServices;
using FoodDiary.Persistence.Runtime.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal static class ModuleContextSaveCoordinator {
    public static async Task SaveAsync(PersistenceSession session, ILogger logger, CancellationToken cancellationToken = default) {
        DbContext context = session.RootContext;
        DbContext[] participants = [.. new[] { context }.Concat(session.Contexts).OrderBy(session.GetSaveOrder)];
        if (!context.Database.IsRelational()) {
            if (participants.Count(participant => participant.ChangeTracker.HasChanges()) > 1) {
                throw new InvalidOperationException("Atomic saves across contexts require a relational provider.");
            }
            await SaveParticipantsAsync(session, participants, cancellationToken);
            AcceptChanges(participants);
            return;
        }

        if (context.Database.CurrentTransaction is not null) {
            await SaveInTransactionAsync(session, participants, context.Database.CurrentTransaction, logger, cancellationToken);
        } else {
            await context.Database.CreateExecutionStrategy().ExecuteAsync(async token => {
                IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(token);
                await using (transaction) {
                    await SaveInTransactionAsync(session, participants, transaction, logger, token);
                    await VerifiedTransactionCommit.CommitAsync(transaction, token);
                }
            }, cancellationToken);
        }
        AcceptChanges(participants);
    }

    private static async Task SaveInTransactionAsync(
        PersistenceSession session,
        DbContext[] participants,
        IDbContextTransaction transaction,
        ILogger logger,
        CancellationToken cancellationToken) {
        DbContext context = session.RootContext;
        var previousTransactions = participants.Where(participant => !ReferenceEquals(participant, context))
            .ToDictionary(participant => participant, participant => session.ActiveTransaction is null
                ? null : participant.Database.CurrentTransaction?.GetDbTransaction());
        const string savepoint = "module_unit_of_work";
        await transaction.CreateSavepointAsync(savepoint, cancellationToken);
        ExceptionDispatchInfo? saveFailure = null;
        try {
            foreach (DbContext module in participants.Where(participant => !ReferenceEquals(participant, context))) {
                await module.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
            }
            await SaveParticipantsAsync(session, participants, cancellationToken);
            await transaction.ReleaseSavepointAsync(savepoint, cancellationToken);
        } catch (Exception exception) {
            saveFailure = ExceptionDispatchInfo.Capture(exception);
            try {
                await transaction.RollbackToSavepointAsync(savepoint, CancellationToken.None);
            } catch (Exception rollbackException) {
                logger.LogWarning(rollbackException, "Could not roll back module savepoint; preserving the original persistence failure.");
            }
        } finally {
            foreach (DbContext module in participants.Where(participant => !ReferenceEquals(participant, context))) {
                await module.Database.UseTransactionAsync(previousTransactions[module], CancellationToken.None);
            }
        }
        // Preserve the original stack after cleanup without an unreachable async catch exit.
        saveFailure?.Throw();
    }

    private static async Task SaveParticipantsAsync(PersistenceSession session, DbContext[] participants, CancellationToken cancellationToken) {
        session.IsSaving = true;
        try {
            foreach (DbContext participant in participants) {
                if (ReferenceEquals(participant, session.RootContext) || participant.ChangeTracker.HasChanges()) {
                    await participant.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);
                }
            }
        } finally {
            session.IsSaving = false;
        }
    }

    private static void AcceptChanges(DbContext[] participants) {
        foreach (DbContext participant in participants) {
            participant.ChangeTracker.AcceptAllChanges();
            DomainEventDispatcher.ClearDomainEvents(participant);
        }
    }
}
