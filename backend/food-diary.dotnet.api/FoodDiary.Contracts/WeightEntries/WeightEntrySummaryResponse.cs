using System;

namespace FoodDiary.Contracts.WeightEntries;

public record WeightEntrySummaryResponse(
    DateTime DateFrom,
    DateTime DateTo,
    double AverageWeight);
