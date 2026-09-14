using FoodDiary.Persistence.Abstractions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.Persistence;

internal sealed class MealPlanningUserDataPurgeParticipant(MealPlanningDbContext context, IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant {
    public int Order => 60;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        await context.ShoppingLists.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
