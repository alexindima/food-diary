namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CyclePredictionsHttpResponse(
    DateTime? NextPeriodStartFrom,
    DateTime? NextPeriodStartTo,
    DateTime? OvulationFrom,
    DateTime? OvulationTo,
    DateTime? PmsWindowStart,
    DateTime? PmsWindowEnd,
    string Confidence,
    string Rationale);
