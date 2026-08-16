namespace FoodDiary.Presentation.Api.Features.Cycles.Requests;

public sealed record UpdateMenstrualEpisodeHttpRequest(
    DateTime StartDate,
    DateTime? EndDate,
    bool? ExcludedFromPredictions = null);
