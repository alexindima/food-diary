namespace FoodDiary.Presentation.Api.Features.Recipes.Responses;

public sealed record RecipeIngredientHttpResponse(
    Guid Id,
    double Amount,
    Guid? ProductId,
    string? ProductName,
    string? ProductBaseUnit,
    double? ProductBaseAmount,
    double? ProductCaloriesPerBase,
    double? ProductProteinsPerBase,
    double? ProductFatsPerBase,
    double? ProductCarbsPerBase,
    double? ProductFiberPerBase,
    double? ProductAlcoholPerBase,
    Guid? NestedRecipeId,
    string? NestedRecipeName);
