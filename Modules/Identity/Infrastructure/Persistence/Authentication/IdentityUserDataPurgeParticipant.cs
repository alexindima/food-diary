using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Authentication;

internal sealed class IdentityUserDataPurgeParticipant(FoodDiaryDbContext context) : IUserDataPurgeParticipant {
    public int Order => 130;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Set<TelegramOperation>().Where(item => item.UserId == userId.Value)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
