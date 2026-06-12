namespace FoodDiary.MailInbox.Domain.Common;

internal static class DomainTime {
    public static DateTime UtcNow => TimeProvider.System.GetUtcNow().UtcDateTime;
}
