using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Admin.Infrastructure.Persistence;

internal sealed class AdminUserDataPurgeParticipant(AdminDbContext context, IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant {
    public int Order => 30;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        await context.AdminImpersonationSessions.Where(item => item.ActorUserId == userId || item.TargetUserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
