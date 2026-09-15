using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Persistence;

internal sealed class DietologistUserDataPurgeParticipant(DietologistDbContext context, IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant {
    public int Order => 40;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        await context.ClientTasks.Where(item => item.ClientUserId == userId || item.DietologistUserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
