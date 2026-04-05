namespace FoodDiary.Presentation.Api.Features.Wearables.Responses;

public sealed record WearableDailySummaryHttpResponse(
    DateTime Date,
    double? Steps,
    double? HeartRate,
    double? CaloriesBurned,
    double? ActiveMinutes,
    double? SleepMinutes);
