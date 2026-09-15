namespace FoodDiary.Modules.Cycles.Presentation.Requests;

public sealed record BleedingLogHttpModel(
    int Type,
    int Flow,
    int? PainImpact,
    string? Notes,
    bool ClearNotes);
