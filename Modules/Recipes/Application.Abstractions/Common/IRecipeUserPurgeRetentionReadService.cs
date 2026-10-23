using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Abstractions.Common;

/// <summary>Returns the owned recipe closure required by surviving consumers during account purge.</summary>
public interface IRecipeUserPurgeRetentionReadService {
    Task<IReadOnlyCollection<RecipeId>> GetRetainedRecipeIdsAsync(UserId sourceUserId, CancellationToken cancellationToken);
}
