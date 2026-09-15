using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadRecipeFavoriteStatus;

public sealed class ReadRecipeFavoriteStatusQueryHandler(IFavoriteRecipeReadModelRepository favoriteRecipeReadModelRepository) : IQueryHandler<ReadRecipeFavoriteStatusQuery, bool> {
    public Task<bool> Handle(ReadRecipeFavoriteStatusQuery request, CancellationToken cancellationToken) {
        RecipeId recipeId = request.RecipeId;
        UserId userId = request.UserId;
        return favoriteRecipeReadModelRepository.ExistsByRecipeIdAsync(recipeId, userId, cancellationToken);
    }

}
