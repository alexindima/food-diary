namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record BleedingEntryHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime Date,
    int Type,
    int Flow,
    int? PainImpact,
    string? Notes);
