namespace FoodDiary.Application.Abstractions.WeightEntries.Models;

public sealed record WeightEntrySummaryModel(
    DateTime StartDate,
    DateTime EndDate,
    double AverageWeightKg);
