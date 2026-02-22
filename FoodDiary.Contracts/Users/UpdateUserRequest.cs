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
    int? StepGoal,
    double? HydrationGoal,
    string? Language,
    string? ProfileImage,
    Guid? ProfileImageAssetId,
    DashboardLayoutSettings? DashboardLayout,
    bool? IsActive
);
