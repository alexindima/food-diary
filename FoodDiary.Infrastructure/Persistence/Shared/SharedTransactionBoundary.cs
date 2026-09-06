using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Shared;

/// <summary>Top-level transaction runners own a clean unit of work, including any owner capabilities they invoke.</summary>
internal static class SharedTransactionBoundary {
    public static async Task<T> ExecuteAttemptAsync<T>(
        FoodDiaryDbContext context,
        IPostCommitActionQueue? postCommitActionQueue,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        Reset(context, postCommitActionQueue);
        try {
            T result = await operation().ConfigureAwait(false);
            if (result is FoodDiary.Results.Result { IsFailure: true }) {
                Reset(context, postCommitActionQueue);
            }
            return result;
        } catch {
            Reset(context, postCommitActionQueue);
            throw;
        }
    }

    private static void Reset(FoodDiaryDbContext context, IPostCommitActionQueue? postCommitActionQueue) {
        DomainEventDispatcher.ClearDomainEvents(context);
        context.ChangeTracker.Clear();
        postCommitActionQueue?.Discard();
    }

    public static void EnsureCleanEntry(FoodDiaryDbContext context, IPostCommitActionQueue? postCommitActionQueue = null) {
        if (postCommitActionQueue?.HasActions == true) {
            throw new InvalidOperationException("A top-level transaction cannot inherit pending post-commit actions.");
        }
        if (context.ChangeTracker.HasChanges()) {
            throw new InvalidOperationException("A top-level transaction cannot save pending changes from its caller. Enter the transaction before mutating tracked entities.");
        }

        if (context.Database.IsRelational() && context.Database.CurrentTransaction is not null) {
            throw new InvalidOperationException("A top-level transaction cannot be nested in an existing transaction.");
        }
    }
}
