namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record SymptomLogHttpModel(
    int Category,
    int Intensity,
    IReadOnlyCollection<string> Tags,
    string? Note,
    bool ClearNote);
