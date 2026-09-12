using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Meals;

public sealed class MealRecognitionReceiptRepository(FoodDiaryDbContext context) : IMealRecognitionReceiptRepository {
    public Task<MealRecognitionReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default) =>
        context.Set<MealRecognitionReceipt>().SingleOrDefaultAsync(receipt => receipt.UserId == userId && receipt.OperationId == operationId, cancellationToken);

    public Task<MealRecognitionReceipt?> FindByRecognitionAsync(UserId userId, Guid recognitionId, CancellationToken cancellationToken = default) =>
        context.Set<MealRecognitionReceipt>().SingleOrDefaultAsync(receipt => receipt.UserId == userId && receipt.RecognitionId == recognitionId, cancellationToken);

    public async Task AddAsync(MealRecognitionReceipt receipt, CancellationToken cancellationToken = default) =>
        await context.Set<MealRecognitionReceipt>().AddAsync(receipt, cancellationToken).ConfigureAwait(false);

    public async Task<(Meal Meal, uint Version)?> LockMealForUndoAsync(UserId userId, MealId mealId, CancellationToken cancellationToken = default) {
        if (context.Database.CurrentTransaction is null) {
            throw new InvalidOperationException("Undo requires an open meal recognition transaction.");
        }
        Meal? meal = await context.Set<Meal>()
            .FromSqlInterpolated($"SELECT m.*, m.xmin FROM \"Meals\" AS m WHERE m.\"Id\" = {mealId.Value} AND m.\"UserId\" = {userId.Value} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return meal is null ? null : (meal, context.Entry(meal).Property<uint>("xmin").CurrentValue);
    }
}
