using FoodDiary.Presentation.Api.Features.Users.Models;

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
    string? Theme,
    string? UiStyle,
    bool? PushNotificationsEnabled,
    bool? FastingPushNotificationsEnabled,
    bool? SocialPushNotificationsEnabled,
    string? ProfileImage,
    Guid? ProfileImageAssetId,
    DashboardLayoutHttpModel? DashboardLayout,
    bool? IsActive);
