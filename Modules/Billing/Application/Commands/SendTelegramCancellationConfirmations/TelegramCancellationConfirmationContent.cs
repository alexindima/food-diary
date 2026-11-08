using System.Globalization;

namespace FoodDiary.Modules.Billing.Application.Commands.SendTelegramCancellationConfirmations;

internal static class TelegramCancellationConfirmationContent {
    public static string Render(DateTime accessUntilUtc, string? language, string? timeZoneId) {
        if (accessUntilUtc.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("A UTC access end is required.", nameof(accessUntilUtc));
        }
        bool russian = string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase) ||
            language?.StartsWith("ru-", StringComparison.OrdinalIgnoreCase) is true;
        TimeZoneInfo zone;
        try {
            zone = string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            zone = TimeZoneInfo.Utc;
        }
        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(accessUntilUtc, zone);
        TimeSpan offset = zone.GetUtcOffset(accessUntilUtc);
        string sign = offset < TimeSpan.Zero ? "-" : "+";
        string date = local.ToString("g", CultureInfo.GetCultureInfo(russian ? "ru-RU" : "en-US"));
        string end = date + $" (UTC{sign}{offset.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture)})";
        return russian
            ? $"Автопродление Premium в FoodDiary отключено.\nPremium действует до {end}.\nПодписка больше не будет продлеваться автоматически."
            : $"FoodDiary Premium auto-renewal is off.\nPremium remains available until {end}.\nThis subscription will not renew automatically.";
    }
}
