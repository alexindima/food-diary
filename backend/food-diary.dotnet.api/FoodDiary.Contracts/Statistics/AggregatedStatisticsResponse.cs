using System;

namespace FoodDiary.Contracts.Statistics;

public record AggregatedStatisticsResponse(
    DateTime DateFrom,
    DateTime DateTo,
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber);
