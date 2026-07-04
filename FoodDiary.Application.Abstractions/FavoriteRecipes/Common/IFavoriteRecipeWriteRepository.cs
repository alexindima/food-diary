using FoodDiary.Domain.Entities.FavoriteRecipes;

namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeWriteRepository : IFavoriteRecipeReadRepository {
    Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);
}
