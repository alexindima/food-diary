using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

public sealed class HydrationOperationReceiptRepository(DbSet<HydrationOperationReceipt> receipts) : IHydrationOperationReceiptRepository {
    public Task<HydrationOperationReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default) =>
        receipts.SingleOrDefaultAsync(receipt => receipt.UserId == userId && receipt.OperationId == operationId, cancellationToken);

    public Task AddAsync(HydrationOperationReceipt receipt, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        receipts.Add(receipt);
        return Task.CompletedTask;
    }
}
