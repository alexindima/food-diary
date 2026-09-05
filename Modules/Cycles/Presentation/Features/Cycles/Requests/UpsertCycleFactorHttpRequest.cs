namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record UpsertCycleFactorHttpRequest(
    int Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes,
    bool ClearNotes);
