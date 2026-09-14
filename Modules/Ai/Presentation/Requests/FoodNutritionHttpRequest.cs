using FoodDiary.Modules.Ai.Presentation.Models;

namespace FoodDiary.Modules.Ai.Presentation.Requests;

public sealed record FoodNutritionHttpRequest(IReadOnlyList<FoodVisionItemHttpModel> Items);
