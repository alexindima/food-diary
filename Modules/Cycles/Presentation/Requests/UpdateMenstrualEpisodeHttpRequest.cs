namespace FoodDiary.Modules.Cycles.Presentation.Requests;

public sealed record UpdateMenstrualEpisodeHttpRequest(
    DateTime StartDate,
    DateTime? EndDate,
    bool? ExcludedFromPredictions = null);
