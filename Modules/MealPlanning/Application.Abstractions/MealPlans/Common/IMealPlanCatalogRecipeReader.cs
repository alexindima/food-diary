using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanCatalogRecipeReader {
    Task<IReadOnlyList<CatalogRecipeReadModel>> SearchAsync(string? search, int limit, CancellationToken cancellationToken);
    Task<IReadOnlySet<Guid>> GetPublicIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
