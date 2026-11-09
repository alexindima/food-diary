using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Persistence;

internal sealed class HydrationUserDataPurgeParticipant(
    HydrationDbContext context,
    IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant, ITelegramDiaryContentTransfer {
    public int Order => 80;

    public async Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.HydrationEntries.Where(item => item.UserId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
    }

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.HydrationEntries.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
