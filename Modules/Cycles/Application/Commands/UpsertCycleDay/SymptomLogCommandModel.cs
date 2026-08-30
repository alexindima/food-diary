namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public sealed record SymptomLogCommandModel(
    int Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note,
    bool ClearNote);
