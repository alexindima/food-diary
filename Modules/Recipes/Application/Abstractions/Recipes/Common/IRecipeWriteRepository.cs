using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeWriteRepository {
    Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
