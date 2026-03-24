using FoodDiary.Contracts.Cycles;

namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record UpsertCycleDayHttpRequest(
    DateTime Date,
    bool IsPeriod,
    DailySymptomsDto Symptoms,
    string? Notes);
