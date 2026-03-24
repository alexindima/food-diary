using FoodDiary.Presentation.Api.Features.Users.Models;

namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record UserHttpResponse(
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
    string? Language,
    string? ProfileImage,
    Guid? ProfileImageAssetId,
    DashboardLayoutHttpModel? DashboardLayout,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime? LastLoginAtUtc);
