using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Cycles.Infrastructure.Persistence;

internal sealed class CyclesUserDataPurgeParticipant(FoodDiaryDbContext context) : IUserDataPurgeParticipant {
    public int Order => 100;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.CycleBleedingEntries.Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.CycleSymptomEntries.Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.CycleFactors.Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.FertilitySignals.Where(item => item.CycleProfile.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.CycleProfiles.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
