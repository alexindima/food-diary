using System.Data;
using System.Diagnostics.Metrics;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class EfReadSnapshotExecutor(SharedPersistenceDbContext context, ReadSnapshotBudgetInterceptor budgetInterceptor,
    TimeProvider timeProvider) : IReadSnapshotExecutor {
    private static readonly Meter Meter = new("FoodDiary.Persistence.ReadSnapshots");
    private static readonly Histogram<int> QueryCount = Meter.CreateHistogram<int>("fooddiary.read_snapshot.queries");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("fooddiary.read_snapshot.duration", "ms");

    public async Task<T> ExecuteAsync<T>(ReadSnapshotBudget budget, Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(operation);
        if (budget.Timeout <= TimeSpan.Zero || budget.MaximumQueries <= 0) {
            throw new ArgumentOutOfRangeException(nameof(budget));
        }
        SharedTransactionBoundary.EnsureCleanEntry(context);
        using var deadline = new CancellationTokenSource(budget.Timeout, timeProvider);
        using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        long started = timeProvider.GetTimestamp();
        try {
            return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () => {
                budgetInterceptor.MaximumQueries = null;
                budgetInterceptor.QueryCount = 0;
                IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, bounded.Token).ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false)) {
                    await context.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", bounded.Token).ConfigureAwait(false);
                    budgetInterceptor.QueryCount = 0;
                    budgetInterceptor.MaximumQueries = budget.MaximumQueries;
                    return await context.Session.WithTransactionAsync(transaction.GetDbTransaction(), () => operation(bounded.Token), bounded.Token).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        } finally {
            budgetInterceptor.MaximumQueries = null;
            QueryCount.Record(budgetInterceptor.QueryCount);
            Duration.Record(timeProvider.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
