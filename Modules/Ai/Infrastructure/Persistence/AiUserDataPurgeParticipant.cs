using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Ai.Infrastructure.Persistence;

internal sealed class AiUserDataPurgeParticipant(FoodDiaryDbContext context) : IUserDataPurgeParticipant {
    public int Order => 120;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Set<FoodRecognitionJob>().Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await context.AiUsages.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
