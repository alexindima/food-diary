namespace FoodDiary.Application.Wearables.Common;

internal static class WearableDateRules {
    public static bool IsSupported(DateTime date, TimeProvider timeProvider) {
        DateTime normalizedDate = date.Date;
        DateTime todayUtc = timeProvider.GetUtcNow().UtcDateTime.Date;
        return normalizedDate >= DateTime.UnixEpoch.Date && normalizedDate <= todayUtc;
    }
}
