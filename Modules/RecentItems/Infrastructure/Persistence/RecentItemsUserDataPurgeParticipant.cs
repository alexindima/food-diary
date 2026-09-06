using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class RecentItemsUserDataPurgeParticipant(FoodDiaryDbContext context) : IUserDataPurgeParticipant {
    public int Order => 70;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.RecentItems.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
