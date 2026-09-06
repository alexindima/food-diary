using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Dietologist.Infrastructure.Persistence;

internal sealed class DietologistUserDataPurgeParticipant(FoodDiaryDbContext context) : IUserDataPurgeParticipant {
    public int Order => 40;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.ClientTasks.Where(item => item.ClientUserId == userId || item.DietologistUserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
