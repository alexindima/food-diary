namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record BleedingEntryHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    int Type,
    int Flow,
    int? PainImpact,
    string? Notes);
