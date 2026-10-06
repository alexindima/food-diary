using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanWriteRepository {
    Task<bool> DeletePersonalAsync(MealPlanId id, UserId userId, CancellationToken cancellationToken = default);

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
