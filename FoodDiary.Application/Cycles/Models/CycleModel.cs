namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleModel(
    Guid Id,
    Guid UserId,
    DateTime StartDate,
    int AverageLength,
    int LutealLength,
    string? Notes,
    IReadOnlyCollection<CycleDayModel> Days,
    CyclePredictionsModel? Predictions);
