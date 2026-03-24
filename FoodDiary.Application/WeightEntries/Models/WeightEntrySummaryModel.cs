namespace FoodDiary.Application.WeightEntries.Models;

public sealed record WeightEntrySummaryModel(
    DateTime StartDate,
    DateTime EndDate,
    double AverageWeight);
