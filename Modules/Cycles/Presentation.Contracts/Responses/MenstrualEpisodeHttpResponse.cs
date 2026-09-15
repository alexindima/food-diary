namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record MenstrualEpisodeHttpResponse(
    Guid Id,
    Guid CycleProfileId,
    DateTime StartDate,
    DateTime? EndDate,
    int Status,
    bool ExcludedFromPredictions);
