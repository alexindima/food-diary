using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Cycles.Infrastructure.Persistence;

internal sealed class CyclesUserDataPurgeParticipant(
    CyclesDbContext context,
    IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant {
    public int Order => 100;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        await context.Set<BleedingEntry>().Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Set<CycleSymptomEntry>().Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Set<CycleFactor>().Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.Set<FertilitySignal>().Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.CycleProfiles.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
