namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public sealed record BleedingLogCommandModel(
    int Type,
    int Flow,
    int? PainImpact,
    string? Notes,
    bool ClearNotes);
