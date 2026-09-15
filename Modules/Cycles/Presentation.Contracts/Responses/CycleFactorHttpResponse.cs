namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record CycleFactorHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    int Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes);
