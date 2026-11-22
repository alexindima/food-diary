using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public interface IRecipePublicationApplier {
    Task<Result> ApplyAsync(
        RecipeId recipeId, UserId ownerId, RecipePublicationClaim claim, string changeJson, CancellationToken cancellationToken);
}
