using FoodDiary.Modules.Users.Presentation.Contracts.Models;

namespace FoodDiary.Modules.Users.Presentation.Requests;

public sealed record UpdateUserHttpRequest(
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? WeightKg,
    double? HeightCm,
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
    bool? IsActive,
    string? TimeZoneId = null);
