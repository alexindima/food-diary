using FoodDiary.Presentation.Api.Features.Cycles.Models;

namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record UpsertCycleDayHttpRequest(
    DateTime Date,
    bool IsPeriod,
    DailySymptomsHttpModel Symptoms,
    string? Notes);
