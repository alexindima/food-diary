using System.Text.Json.Serialization;
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
    string? TimeZoneId = null) {
    private DateTime? _birthDate = BirthDate;
    private string? _profileImage = ProfileImage;
    private Guid? _profileImageAssetId = ProfileImageAssetId;

    [JsonConstructor]
    public UpdateUserHttpRequest() : this(
        Username: null,
        FirstName: null,
        LastName: null,
        BirthDate: null,
        Gender: null,
        WeightKg: null,
        HeightCm: null,
        ActivityLevel: null,
        StepGoal: null,
        HydrationGoal: null,
        Language: null,
        Theme: null,
        UiStyle: null,
        PushNotificationsEnabled: null,
        FastingPushNotificationsEnabled: null,
        SocialPushNotificationsEnabled: null,
        ProfileImage: null,
        ProfileImageAssetId: null,
        DashboardLayout: null,
        IsActive: null) {
    }

    /// <summary>Omit to preserve the current date, or send null to clear it.</summary>
    public DateTime? BirthDate {
        get => _birthDate;
        init {
            _birthDate = value;
            BirthDateSpecified = true;
        }
    }

    [JsonIgnore]
    public bool BirthDateSpecified { get; private init; } = BirthDate.HasValue;

    /// <summary>Omit both image fields to preserve the avatar, or send null to clear it.</summary>
    public string? ProfileImage {
        get => _profileImage;
        init {
            _profileImage = value;
            ProfileImageSpecified = true;
        }
    }

    public Guid? ProfileImageAssetId {
        get => _profileImageAssetId;
        init {
            _profileImageAssetId = value;
            ProfileImageSpecified = true;
        }
    }

    [JsonIgnore]
    public bool ProfileImageSpecified { get; private init; } = ProfileImage is not null || ProfileImageAssetId.HasValue;
}
