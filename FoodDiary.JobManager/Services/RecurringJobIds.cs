namespace FoodDiary.JobManager.Services;

public static class RecurringJobIds {
    public const string ImageAssetsCleanup = "image-assets-cleanup";
    public const string NotificationsCleanup = "notifications-cleanup";
    public const string UsersCleanup = "users-cleanup";
    public const string BillingRenewal = "billing-renewal";
    public const string FastingNotifications = "fasting-notifications";
    public const string ImageObjectDeletionOutbox = "image-object-deletion-outbox";
    public const string EmailOutbox = "email-outbox";
    public const string NotificationWebPushOutbox = "notification-web-push-outbox";
    public const string UserLoginEventsCleanup = "user-login-events-cleanup";

    public static readonly string[] All = [
        ImageAssetsCleanup,
        NotificationsCleanup,
        UsersCleanup,
        BillingRenewal,
        FastingNotifications,
        ImageObjectDeletionOutbox,
        EmailOutbox,
        NotificationWebPushOutbox,
        UserLoginEventsCleanup,
    ];
}
