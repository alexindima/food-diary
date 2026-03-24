namespace FoodDiary.Application.Statistics.Models;

public sealed record AggregatedStatisticsModel(
    DateTime DateFrom,
    DateTime DateTo,
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber);
