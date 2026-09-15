namespace FoodDiary.Modules.Wearables.Presentation.Responses;

public sealed record WearableDailySummaryHttpResponse(
    DateTime Date,
    double? Steps,
    double? HeartRate,
    double? CaloriesBurned,
    double? ActiveMinutes,
    double? SleepMinutes);
