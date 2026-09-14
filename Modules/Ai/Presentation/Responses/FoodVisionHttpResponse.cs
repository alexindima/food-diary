using FoodDiary.Modules.Ai.Presentation.Models;

namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record FoodVisionHttpResponse(
    IReadOnlyList<FoodVisionItemHttpModel> Items,
    string? Notes = null);
