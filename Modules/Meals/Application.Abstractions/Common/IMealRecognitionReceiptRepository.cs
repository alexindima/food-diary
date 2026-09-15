using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public interface IMealRecognitionReceiptRepository {
    Task<MealRecognitionReceipt?> FindAsync(UserId userId, Guid operationId, CancellationToken cancellationToken = default);
    Task<MealRecognitionReceipt?> FindByRecognitionAsync(UserId userId, Guid recognitionId, CancellationToken cancellationToken = default);
    Task AddAsync(MealRecognitionReceipt receipt, CancellationToken cancellationToken = default);
    Task<(Meal Meal, uint Version)?> LockMealForUndoAsync(UserId userId, MealId mealId, CancellationToken cancellationToken = default);
}
