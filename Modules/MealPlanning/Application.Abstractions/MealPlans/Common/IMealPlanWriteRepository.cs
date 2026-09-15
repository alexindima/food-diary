using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanWriteRepository {
    Task<MealPlan> AddAsync(MealPlan plan, CancellationToken cancellationToken = default);

    Task<MealPlan?> GetByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default);

    Task<MealPlan?> GetCuratedByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default);
}
