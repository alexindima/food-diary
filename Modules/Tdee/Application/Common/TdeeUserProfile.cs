namespace FoodDiary.Application.Tdee.Common;

public sealed record TdeeUserProfile(
    double? Bmr,
    double? EstimatedTdee,
    double? Weight,
    double? DesiredWeight,
    double? DailyCalorieTarget);
