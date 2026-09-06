using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class AdminUserDataPurgeParticipant(FoodDiaryDbContext context) : IUserDataPurgeParticipant {
    public int Order => 30;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.AdminImpersonationSessions.Where(item => item.ActorUserId == userId || item.TargetUserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
