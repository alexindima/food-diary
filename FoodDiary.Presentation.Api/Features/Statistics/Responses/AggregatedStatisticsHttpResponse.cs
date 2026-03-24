namespace FoodDiary.Presentation.Api.Features.Statistics.Responses;

public sealed record AggregatedStatisticsHttpResponse(
    DateTime DateFrom,
    DateTime DateTo,
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber);
