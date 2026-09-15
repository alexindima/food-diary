using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Mappings;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipes;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadFavoriteRecipes;

public sealed class ReadFavoriteRecipesQueryHandler(IFavoriteRecipeReadModelRepository favoriteRecipeReadModelRepository) : IQueryHandler<ReadFavoriteRecipesQuery, IReadOnlyList<FavoriteRecipeModel>> {
    public async Task<IReadOnlyList<FavoriteRecipeModel>> Handle(ReadFavoriteRecipesQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        IReadOnlyList<FavoriteRecipeReadModel> favorites = await favoriteRecipeReadModelRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

}
