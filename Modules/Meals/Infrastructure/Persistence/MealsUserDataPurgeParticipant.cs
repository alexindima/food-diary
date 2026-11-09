using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Meals.Infrastructure.Persistence;

internal sealed class MealsUserDataPurgeParticipant(MealsDbContext context, IModuleTransactionCoordinator coordinator) : IUserDataPurgeParticipant, ITelegramDiaryContentTransfer {
    public int Order => 5;

    public async Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.Meals.Where(item => item.UserId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
    }

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await context.MealItems.Where(item => item.Meal.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.Meals.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
