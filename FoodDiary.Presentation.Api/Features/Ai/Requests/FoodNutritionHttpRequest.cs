using FoodDiary.Contracts.Ai;

namespace FoodDiary.Presentation.Api.Features.Ai.Requests;

public sealed record FoodNutritionHttpRequest(IReadOnlyList<FoodVisionItem> Items);
