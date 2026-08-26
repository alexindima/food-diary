namespace FoodDiary.Web.Api.Options;

public sealed class NotificationTestSchedulerOptions {
    public const string SectionName = "NotificationTestScheduler";

    public int MaxPending { get; init; } = 1000;

    public static bool HasValidMaxPending(NotificationTestSchedulerOptions options) => options.MaxPending > 0;
}
