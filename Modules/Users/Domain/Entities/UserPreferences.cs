using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Entities;

public sealed class UserPreferences : AggregateRoot<UserId> {
    public string? DashboardLayoutJson { get; internal set; }
    public string? Language { get; internal set; }
    public string? Theme { get; internal set; }
    public string? UiStyle { get; internal set; }
    public string SurfaceStyle { get; internal set; } = SurfaceStyleCode.Default.Value;
    public bool PushNotificationsEnabled { get; internal set; }
    public bool FastingPushNotificationsEnabled { get; internal set; }
    public bool SocialPushNotificationsEnabled { get; internal set; }
    public int FastingCheckInReminderHours { get; internal set; }
    public int FastingCheckInFollowUpReminderHours { get; internal set; }
    public string? TimeZoneId { get; internal set; }

    private UserPreferences() { }

    internal static UserPreferences Create(UserId userId) {
        var state = new UserPreferences { Id = userId };
        state.SetCreated();
        return state;
    }

    internal void Touch() => SetModified();
}
