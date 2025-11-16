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
    double? Height,
    string ActivityLevel,
    double? DailyCalorieTarget,
    double? ProteinTarget,
    double? FatTarget,
    double? CarbTarget,
    int? StepGoal,
    double? WaterGoal,
    string? ProfileImage,
    bool IsActive
);
