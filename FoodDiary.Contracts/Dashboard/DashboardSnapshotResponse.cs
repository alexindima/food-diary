using System;
using System.Collections.Generic;
using FoodDiary.Contracts.Consumptions;

namespace FoodDiary.Contracts.Dashboard;

public record DashboardSnapshotResponse(
    DateTime Date,
    double DailyGoal,
    DashboardStatisticsDto Statistics,
    IReadOnlyList<DailyCaloriesDto> WeeklyCalories,
    DashboardWeightDto Weight,
    DashboardWaistDto Waist,
    DashboardMealsDto Meals);

public record DashboardStatisticsDto(
    double TotalCalories,
    double AverageProteins,
    double AverageFats,
    double AverageCarbs,
    double AverageFiber,
    double? ProteinGoal,
    double? FatGoal,
    double? CarbGoal,
    double? FiberGoal);

public record DashboardWeightDto(
    WeightEntryDto? Latest,
    WeightEntryDto? Previous,
    double? Desired);

public record DashboardWaistDto(
    WaistEntryDto? Latest,
    WaistEntryDto? Previous,
    double? Desired);

public record DashboardMealsDto(
    IReadOnlyList<ConsumptionResponse> Items,
    int Total);

public record DailyCaloriesDto(DateTime Date, double Calories);

public record WeightEntryDto(DateTime Date, double Weight);

public record WaistEntryDto(DateTime Date, double Circumference);
