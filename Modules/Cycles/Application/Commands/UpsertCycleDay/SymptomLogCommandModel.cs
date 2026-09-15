namespace FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleDay;

public sealed record SymptomLogCommandModel(
    int Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note,
    bool ClearNote);
