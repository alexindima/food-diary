namespace FoodDiary.Modules.Statistics.Presentation.Responses;

public sealed record AggregatedStatisticsHttpResponse(
    DateTime DateFrom,
    DateTime DateTo,
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber,
    double TotalProteins,
    double TotalFats,
    double TotalCarbs,
    double TotalFiber,
    double BreakfastCalories,
    double LunchCalories,
    double DinnerCalories,
    double SnackCalories,
    int MealCount,
    int TrackedDayCount);
