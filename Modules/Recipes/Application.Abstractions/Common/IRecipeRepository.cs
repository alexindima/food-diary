namespace FoodDiary.Modules.Recipes.Application.Abstractions.Common;

public interface IRecipeRepository : IRecipeReadRepository, IRecipeWriteRepository, IRecipeNutritionWriter;
