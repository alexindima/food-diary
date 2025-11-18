using System;

namespace FoodDiary.Contracts.WaistEntries;

public record WaistEntrySummaryResponse(
    DateTime DateFrom,
    DateTime DateTo,
    double AverageCircumference);
