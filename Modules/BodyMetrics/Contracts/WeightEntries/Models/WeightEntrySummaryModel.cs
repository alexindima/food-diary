namespace FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

public sealed record WeightEntrySummaryModel(
    DateTime StartDate,
    DateTime EndDate,
    double AverageWeightKg);
