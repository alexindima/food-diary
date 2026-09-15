using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Meals.Infrastructure.Persistence.Meals;

public sealed class MealRecognitionReceiptRepository(MealsDbContext context, Func<CancellationToken, Task> synchronizeTransactionAsync) : IMealRecognitionReceiptRepository {
    public async Task<MealRecognitionReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default) {
        await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await context.Set<MealRecognitionReceipt>().SingleOrDefaultAsync(receipt => receipt.UserId == userId && receipt.OperationId == operationId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MealRecognitionReceipt?> FindByRecognitionAsync(UserId userId, Guid recognitionId, CancellationToken cancellationToken = default) {
        await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await context.Set<MealRecognitionReceipt>().SingleOrDefaultAsync(receipt => receipt.UserId == userId && receipt.RecognitionId == recognitionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(MealRecognitionReceipt receipt, CancellationToken cancellationToken = default) =>
        await context.Set<MealRecognitionReceipt>().AddAsync(receipt, cancellationToken).ConfigureAwait(false);

    public async Task<(Meal Meal, uint Version)?> LockMealForUndoAsync(UserId userId, MealId mealId, CancellationToken cancellationToken = default) {
        await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (context.Database.CurrentTransaction is null) {
            throw new InvalidOperationException("Undo requires an open meal recognition transaction.");
        }
        Meal? meal = await context.Set<Meal>()
            .FromSqlInterpolated($"SELECT m.*, m.xmin FROM \"Meals\" AS m WHERE m.\"Id\" = {mealId.Value} AND m.\"UserId\" = {userId.Value} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return meal is null ? null : (meal, context.Entry(meal).Property<uint>("xmin").CurrentValue);
    }
}
