namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record CreateConsumptionHttpRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<ConsumptionItemHttpRequest> Items,
    IReadOnlyList<ConsumptionAiSessionHttpRequest>? AiSessions = null,
    bool IsNutritionAutoCalculated = true,
    double? ManualCalories = null,
    double? ManualProteins = null,
    double? ManualFats = null,
    double? ManualCarbs = null,
    double? ManualFiber = null,
    double? ManualAlcohol = null,
    int PreMealSatietyLevel = 3,
    int PostMealSatietyLevel = 3);
