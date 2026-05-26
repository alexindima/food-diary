namespace FoodDiary.Application.Statistics.Models;

public sealed record AggregatedStatisticsModel(
    DateTime DateFrom,
    DateTime DateTo,
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber,
    double TotalProteins = 0,
    double TotalFats = 0,
    double TotalCarbs = 0,
    double TotalFiber = 0);
