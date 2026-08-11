namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserTdeeProfileModel(
    double? Bmr,
    double? EstimatedTdee,
    double? Weight,
    double? DesiredWeight,
    double? DailyCalorieTarget);
