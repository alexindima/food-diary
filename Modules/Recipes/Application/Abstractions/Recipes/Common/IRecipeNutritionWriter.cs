using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeNutritionWriter {
    Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
