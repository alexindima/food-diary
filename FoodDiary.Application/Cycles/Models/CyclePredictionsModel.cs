namespace FoodDiary.Application.Cycles.Models;

public sealed record CyclePredictionsModel(
    DateTime? NextPeriodStart,
    DateTime? OvulationDate,
    DateTime? PmsStart);
