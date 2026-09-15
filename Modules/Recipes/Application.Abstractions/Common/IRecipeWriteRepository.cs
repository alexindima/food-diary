using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Abstractions.Common;

public interface IRecipeWriteRepository {
    Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
