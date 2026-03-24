using FoodDiary.Contracts.Users;

namespace FoodDiary.Presentation.Api.Features.Users.Requests;

public sealed record UpdateUserHttpRequest(
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
    bool? IsActive);
