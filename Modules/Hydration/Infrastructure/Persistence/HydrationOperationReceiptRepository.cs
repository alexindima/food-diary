using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
