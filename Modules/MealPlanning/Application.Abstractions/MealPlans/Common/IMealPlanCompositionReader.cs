using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanCompositionReader {
    Task<MealPlanReadModel?> GetReadModelByIdAsync(MealPlanId id, UserId? viewerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot>> GetRecipeSnapshotsAsync(
        IReadOnlyCollection<RecipeId> ids, UserId? viewerId, CancellationToken cancellationToken = default);
}
