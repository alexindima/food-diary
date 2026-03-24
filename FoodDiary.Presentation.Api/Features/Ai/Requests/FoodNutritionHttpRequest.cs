using FoodDiary.Presentation.Api.Features.Ai.Models;

namespace FoodDiary.Presentation.Api.Features.Ai.Requests;

public sealed record FoodNutritionHttpRequest(IReadOnlyList<FoodVisionItemHttpModel> Items);
