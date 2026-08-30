namespace FoodDiary.Application.Abstractions.Wearables.Models;

public sealed record WearableDailySummaryModel(
    DateTime Date,
    double? Steps,
    double? HeartRate,
    double? CaloriesBurned,
    double? ActiveMinutes,
    double? SleepMinutes);
