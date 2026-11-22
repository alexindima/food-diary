using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public interface IRecipePublicationQueue {
    bool Enabled { get; }

    Task<RecipePublicationState> StageAsync(
        RecipeId recipeId, UserId ownerId, string changeJson, RecipeModerationContent content, CancellationToken cancellationToken);
    Task<RecipePublicationState?> GetCurrentAsync(RecipeId recipeId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<RecipeId, RecipePublicationState>> GetCurrentAsync(
        IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken);
    Task CancelAsync(RecipeId recipeId, CancellationToken cancellationToken);
    Task<bool> CanApplyAsync(RecipeId recipeId, RecipePublicationClaim claim, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(RecipeId recipeId, RecipePublicationClaim claim, CancellationToken cancellationToken);
}
