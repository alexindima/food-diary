namespace FoodDiary.Contracts.Users;

public record UpdateUserRequest(
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? Weight,
    double? Height,
    string? ActivityLevel,
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
    DashboardLayoutSettings? DashboardLayout,
    bool? IsActive
);
