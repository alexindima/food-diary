namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

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
