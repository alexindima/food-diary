using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserProfileUpdate(
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    DateTime? BirthDate = null,
    string? Gender = null,
    double? Weight = null,
    double? Height = null,
    ActivityLevel? ActivityLevel = null,
    int? StepGoal = null,
    double? HydrationGoal = null,
    string? ProfileImage = null,
    ImageAssetId? ProfileImageAssetId = null,
    string? DashboardLayoutJson = null,
    string? Language = null);
