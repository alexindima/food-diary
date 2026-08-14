using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserDashboardProfileModel(
    Guid Id,
    string Email,
    string? Language,
    string? DashboardLayoutJson,
    double? DesiredWeightKg,
    double? DesiredWaistCm,
    double? HydrationGoal,
    double? WaterGoal,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    UserCalorieSchedule CalorieSchedule);
