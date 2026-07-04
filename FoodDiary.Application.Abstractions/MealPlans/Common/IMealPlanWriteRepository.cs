using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.MealPlans.Common;

public interface IMealPlanWriteRepository {
    Task<MealPlan> AddAsync(MealPlan plan, CancellationToken cancellationToken = default);

    Task<MealPlan?> GetByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default);
}
