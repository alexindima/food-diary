namespace FoodDiary.Application.Abstractions.WaistEntries.Models;

public sealed record WaistEntrySummaryModel(
    DateTime StartDate,
    DateTime EndDate,
    double AverageCircumference);
