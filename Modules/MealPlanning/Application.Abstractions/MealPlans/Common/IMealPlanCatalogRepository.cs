using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanCatalogRepository {
    Task<IReadOnlyList<MealPlanSummaryReadModel>> GetPageAsync(int page, int limit, CancellationToken cancellationToken);
    Task<MealPlan?> GetForUpdateAsync(MealPlanId id, CancellationToken cancellationToken);
}
