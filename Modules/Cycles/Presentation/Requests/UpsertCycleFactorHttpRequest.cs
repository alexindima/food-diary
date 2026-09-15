namespace FoodDiary.Modules.Cycles.Presentation.Requests;

public sealed record UpsertCycleFactorHttpRequest(
    int Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes,
    bool ClearNotes);
