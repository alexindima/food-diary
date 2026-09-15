using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Abstractions.Common;

public interface IRecipeNutritionWriter {
    Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default);
}
