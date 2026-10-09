namespace FoodDiary.Modules.Tdee.Application.Common;

public sealed record TdeeUserProfile(
    BasalEnergyKcal? Bmr,
    EstimatedDailyEnergyKcal? EstimatedTdee,
    CalculationMeasuredWeight? Weight,
    CalculationDesiredWeight? DesiredWeight,
    DailyCalorieTargetKcal? DailyCalorieTarget);
