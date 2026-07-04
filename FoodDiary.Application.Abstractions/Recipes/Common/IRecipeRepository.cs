namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeRepository : IRecipeReadRepository, IRecipeWriteRepository, IRecipeNutritionWriter;
