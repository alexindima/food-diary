namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardStatisticsModel(
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber,
    double? ProteinGoal,
    double? FatGoal,
    double? CarbGoal,
    double? FiberGoal);
