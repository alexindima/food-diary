using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;

internal sealed class BodyMetricsUserDataPurgeParticipant(
    BodyMetricsDbContext context,
    IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant, ITelegramDiaryContentTransfer {
    public int Order => 90;

    public async Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.WeightEntries.Where(item => item.UserId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
        await context.WaistEntries.Where(item => item.UserId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
    }

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.WeightEntries.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.WaistEntries.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
