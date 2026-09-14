namespace FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

public sealed record WaistEntrySummaryModel(
    DateTime StartDate,
    DateTime EndDate,
    double AverageCircumferenceCm);
