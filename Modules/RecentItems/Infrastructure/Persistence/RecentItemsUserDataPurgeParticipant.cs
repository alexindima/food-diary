using FoodDiary.Modules.RecentItems.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class RecentItemsUserDataPurgeParticipant(RecentItemsDbContext context, IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant {
    public int Order => 70;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        await context.RecentItems.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
