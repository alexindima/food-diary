namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record ConsumptionItemHttpRequest(
    Guid? ProductId,
    Guid? RecipeId,
    double Amount);
