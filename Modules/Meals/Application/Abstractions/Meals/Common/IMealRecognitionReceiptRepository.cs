using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealRecognitionReceiptRepository {
    Task<MealRecognitionReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default);
    Task<MealRecognitionReceipt?> FindByRecognitionAsync(UserId userId, Guid recognitionId, CancellationToken cancellationToken = default);
    Task AddAsync(MealRecognitionReceipt receipt, CancellationToken cancellationToken = default);
    Task<(Meal Meal, uint Version)?> LockMealForUndoAsync(UserId userId, MealId mealId, CancellationToken cancellationToken = default);
}
