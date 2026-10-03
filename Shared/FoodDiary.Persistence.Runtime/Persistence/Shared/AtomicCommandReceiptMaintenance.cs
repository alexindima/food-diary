using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class AtomicCommandReceiptMaintenance(SharedPersistenceDbContext context, TimeProvider timeProvider)
    : IAtomicCommandReceiptMaintenance {
    public async Task DeleteOwnerAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Set<AtomicCommandReceipt>().Where(receipt => receipt.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> DeleteExpiredBatchAsync(int batchSize, CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);
        SharedTransactionBoundary.EnsureCleanEntry(context);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        IQueryable<string> expiredKeys = context.Set<AtomicCommandReceipt>().Where(receipt => receipt.ExpiresOnUtc <= now)
            .OrderBy(receipt => receipt.ExpiresOnUtc).ThenBy(receipt => receipt.Key).Take(Math.Min(batchSize, 1000)).Select(receipt => receipt.Key);
        return context.Set<AtomicCommandReceipt>().Where(receipt => expiredKeys.Contains(receipt.Key)).ExecuteDeleteAsync(cancellationToken);
    }
}
