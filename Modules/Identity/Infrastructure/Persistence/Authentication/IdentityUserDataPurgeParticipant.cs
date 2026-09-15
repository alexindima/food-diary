using FoodDiary.Modules.Identity.PersistenceModel.Authentication;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Persistence.Authentication;

internal sealed class IdentityUserDataPurgeParticipant(IdentityDbContext context, IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant {
    public int Order => 130;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        await context.Set<TelegramOperation>().Where(item => item.UserId == userId.Value)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
