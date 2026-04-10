namespace FoodDiary.JobManager.Services;

public static class RecurringJobIds {
    public const string ImageAssetsCleanup = "image-assets-cleanup";
    public const string NotificationsCleanup = "notifications-cleanup";
    public const string UsersCleanup = "users-cleanup";

    public static readonly string[] All = [
        ImageAssetsCleanup,
        NotificationsCleanup,
        UsersCleanup
    ];
}
