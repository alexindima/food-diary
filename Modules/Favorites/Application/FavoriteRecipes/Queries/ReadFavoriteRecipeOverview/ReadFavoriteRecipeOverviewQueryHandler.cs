using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Mappings;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;

namespace FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.ReadFavoriteRecipeOverview;

public sealed class ReadFavoriteRecipeOverviewQueryHandler(IFavoriteRecipeQuery queries)
    : IQueryHandler<ReadFavoriteRecipeOverviewQuery, FavoriteRecipeOverviewModel> {
    public async Task<FavoriteRecipeOverviewModel> Handle(ReadFavoriteRecipeOverviewQuery request, CancellationToken cancellationToken) {
        (IReadOnlyList<FavoriteRecipeReadModel> preview, int total) = await queries.GetPageReadModelsAsync(request.UserId, 1, Math.Clamp(request.PreviewLimit, 0, 50), search: null, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FavoriteRecipeReadModel> items = await queries.GetByRecipeIdsReadModelsAsync(request.UserId, request.RecipeIds, cancellationToken).ConfigureAwait(false);
        return new FavoriteRecipeOverviewModel(items.Select(item => item.ToModel()).ToArray(), preview.Select(item => item.ToModel()).ToArray(), total);
    }
}
