namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record MenstrualEpisodeHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime StartDate,
    DateTime? EndDate,
    int Status,
    bool ExcludedFromPredictions);
