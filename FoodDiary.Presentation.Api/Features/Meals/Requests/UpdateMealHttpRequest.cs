namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record UpdateMealHttpRequest(
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<MealItemHttpRequest> Items,
    IReadOnlyList<MealAiSessionHttpRequest>? AiSessions = null,
    bool IsNutritionAutoCalculated = true,
    double? ManualCalories = null,
    double? ManualProteins = null,
    double? ManualFats = null,
    double? ManualCarbs = null,
    double? ManualFiber = null,
    double? ManualAlcohol = null,
    int PreMealSatietyLevel = 3,
    int PostMealSatietyLevel = 3);
