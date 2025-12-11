using System;

namespace FoodDiary.Contracts.Users;

public record UserResponse(
    Guid Id,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? Weight,
    double? DesiredWeight,
    double? DesiredWaist,
    double? Height,
    string ActivityLevel,
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    double? FiberTarget,
    int? StepGoal,
    double? WaterGoal,
    double? HydrationGoal,
    string? ProfileImage,
    bool IsActive
);
