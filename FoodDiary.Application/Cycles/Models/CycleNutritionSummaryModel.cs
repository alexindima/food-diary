namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleNutritionSummaryModel(
    DateTime DateFrom,
    DateTime DateTo,
    int LoggedCycleDays,
    int DaysWithMeals,
    int BleedingDays,
    double AverageCaloriesOnBleedingDays,
    double AverageCaloriesOnNonBleedingCycleDays,
    double AverageFiberOnBleedingDays,
    double AverageFiberOnNonBleedingCycleDays,
    double AveragePainImpactOnDaysWithMeals);
