namespace FoodDiary.Modules.Recipes.Presentation.Responses;

public sealed record PublicRecipeStepHttpResponse(int StepNumber, string? Title, string Instruction,
    IReadOnlyList<string> Images, IReadOnlyList<PublicRecipeIngredientHttpResponse> Ingredients);
