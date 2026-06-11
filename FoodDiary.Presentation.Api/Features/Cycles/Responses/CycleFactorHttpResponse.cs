namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleFactorHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    int Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes);
