namespace FoodDiary.Application.Cycles.Models;

public sealed record CyclePredictionsModel(
    DateTime? NextPeriodStartFrom,
    DateTime? NextPeriodStartTo,
    DateTime? OvulationFrom,
    DateTime? OvulationTo,
    DateTime? PmsWindowStart,
    DateTime? PmsWindowEnd,
    string Confidence,
    string Rationale);
