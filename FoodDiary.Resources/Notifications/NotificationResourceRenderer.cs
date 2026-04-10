using System.Globalization;
using System.Resources;
using FoodDiary.Application.Notifications.Common;

namespace FoodDiary.Resources.Notifications;

public sealed class NotificationResourceRenderer : INotificationTextRenderer {
    private static readonly ResourceManager ResourceManager =
        new("FoodDiary.Resources.Notifications.NotificationTemplates", typeof(NotificationResourceRenderer).Assembly);

    public NotificationText Render(string type, string? locale = null, params object[] arguments) {
        if (string.IsNullOrWhiteSpace(type)) {
            throw new ArgumentException(@"Notification type is required.", nameof(type));
        }

        var culture = ResolveCulture(locale);
        var title = GetRequired($"{type}_Title", culture);
        var body = GetOptional($"{type}_Body", culture);

        return new NotificationText(
            Format(title, culture, arguments),
            body is null ? null : Format(body, culture, arguments));
    }

    public NotificationText RenderFromPayload(string type, string payloadJson, string? locale = null) {
        if (string.IsNullOrWhiteSpace(payloadJson)) {
            throw new ArgumentException("Notification payload is required.", nameof(payloadJson));
        }

        return type switch {
            NotificationTypes.NewRecommendation => RenderNewRecommendation(payloadJson, locale),
            _ => Render(type, locale)
        };
    }

    private static CultureInfo ResolveCulture(string? locale) {
        var normalized = string.IsNullOrWhiteSpace(locale)
            ? "en"
            : locale.Trim().ToLowerInvariant();

        var cultureName = normalized.StartsWith("ru", StringComparison.Ordinal)
            ? "ru"
            : "en";

        try {
            return CultureInfo.GetCultureInfo(cultureName);
        } catch (CultureNotFoundException) {
            return CultureInfo.InvariantCulture;
        }
    }

    private static string GetRequired(string key, CultureInfo culture) =>
        ResourceManager.GetString(key, culture)
        ?? ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en"))
        ?? throw new InvalidOperationException($"Missing notification resource '{key}'.");

    private static string? GetOptional(string key, CultureInfo culture) =>
        ResourceManager.GetString(key, culture)
        ?? ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en"));

    private static string Format(string template, CultureInfo culture, object[] arguments) =>
        arguments.Length == 0 ? template : string.Format(culture, template, arguments);

    private NotificationText RenderNewRecommendation(string payloadJson, string? locale) {
        var payload = NotificationPayloadSerializer.Deserialize<NewRecommendationNotificationPayload>(payloadJson);
        var dietologistName = string.IsNullOrWhiteSpace(payload?.DietologistName)
            ? "Your dietologist"
            : payload.DietologistName;

        return Render(NotificationTypes.NewRecommendation, locale, dietologistName);
    }
}
