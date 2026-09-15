namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardStatisticsHttpResponse(
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber,
    double? ProteinGoal,
    double? FatGoal,
    double? CarbGoal,
    double? FiberGoal);
