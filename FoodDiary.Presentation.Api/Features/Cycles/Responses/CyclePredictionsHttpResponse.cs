namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CyclePredictionsHttpResponse(
    DateTime? NextPeriodStart,
    DateTime? OvulationDate,
    DateTime? PmsStart);
