namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserTdeeProfileModel(
    double? Bmr,
    double? EstimatedTdee,
    double? WeightKg,
    double? DesiredWeightKg,
    double? DailyCalorieTarget);
