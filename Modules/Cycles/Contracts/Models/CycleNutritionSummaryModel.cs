namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleNutritionSummaryModel(
    DateOnly DateFrom,
    DateOnly DateTo,
    int LoggedCycleDays,
    int DaysWithMeals,
    int BleedingDays,
    double AverageCaloriesOnBleedingDays,
    double AverageCaloriesOnNonBleedingCycleDays,
    double AverageFiberOnBleedingDays,
    double AverageFiberOnNonBleedingCycleDays,
    double AveragePainImpactOnDaysWithMeals,
    bool HasEnoughNutritionData,
    bool ConsentRequired = false,
    int CompletedCyclesAnalyzed = 0,
    int ComparableCycles = 0,
    string DataSufficiency = "Insufficient",
    IReadOnlyCollection<string>? ReasonCodes = null,
    string AlgorithmVersion = "nutrition-v2.0");
