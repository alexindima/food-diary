using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.MealPlans.Common;

public interface IMealPlanCompositionReader {
    Task<MealPlanReadModel?> GetReadModelByIdAsync(MealPlanId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot>> GetRecipeSnapshotsAsync(
        IReadOnlyCollection<RecipeId> ids, CancellationToken cancellationToken = default);
}
