namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record BleedingLogHttpModel(
    int Type,
    int Flow,
    int? PainImpact,
    string? Notes,
    bool ClearNotes);
