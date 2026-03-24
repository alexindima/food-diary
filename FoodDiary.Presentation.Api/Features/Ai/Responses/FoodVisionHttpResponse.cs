using FoodDiary.Presentation.Api.Features.Ai.Models;

namespace FoodDiary.Presentation.Api.Features.Ai.Responses;

public sealed record FoodVisionHttpResponse(
    IReadOnlyList<FoodVisionItemHttpModel> Items,
    string? Notes = null);
