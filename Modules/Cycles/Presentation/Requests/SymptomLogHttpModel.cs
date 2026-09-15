namespace FoodDiary.Modules.Cycles.Presentation.Requests;

public sealed record SymptomLogHttpModel(
    int Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note,
    bool ClearNote);
