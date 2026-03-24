using FoodDiary.Presentation.Api.Features.Cycles.Models;

namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleDayHttpResponse(
    Guid Id,
    Guid CycleId,
    DateTime Date,
    bool IsPeriod,
    DailySymptomsHttpModel Symptoms,
    string? Notes);
