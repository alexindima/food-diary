using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Email.Commands.QueueSubscriptionCancellationEmail;

namespace FoodDiary.Modules.Identity.Application.Email.Commands.QueueSubscriptionCancellationEmail;

internal static partial class SubscriptionCancellationEmailContent {
    public const string FeedbackAddress = "feedback@fooddiary.club";

    public static EmailTemplateContent Render(QueueSubscriptionCancellationEmailCommand request, EmailOptions options,
        string locale, EmailTemplateContent? template) {
        if (!Uri.TryCreate(options.FrontendBaseUrl, UriKind.Absolute, out Uri? origin) ||
            origin.Scheme is not ("http" or "https")) {
            throw new InvalidOperationException("Email FrontendBaseUrl is not configured.");
        }
        bool russian = locale is "ru";
        string brand = string.IsNullOrWhiteSpace(options.FromName) ? "FoodDiary" : options.FromName;
        string subscriptionLink = options.FrontendBaseUrl.TrimEnd('/') + "/premium";
        string accessMessage = BuildAccessMessage(request, locale);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["brand"] = brand, ["accessMessage"] = accessMessage,
            ["feedbackEmail"] = FeedbackAddress, ["feedbackLink"] = "mailto:" + FeedbackAddress,
            ["subscriptionLink"] = subscriptionLink, ["link"] = subscriptionLink,
        };
        string subject = russian ? "Автопродление Premium отключено" : "Premium auto-renewal is off";
        string thanks = russian ? "Спасибо, что пользовались Premium в FoodDiary."
            : "Thank you for using FoodDiary Premium.";
        string feedback = russian
            ? "Если вам чего-то не хватило или что-то не понравилось, напишите нам. Ваш отзыв поможет нам улучшить FoodDiary."
            : "If something was missing or did not work well, please tell us. Your feedback helps us improve FoodDiary.";
        string renewal = russian ? "Автопродление отключено. Подписка больше не будет продлеваться автоматически."
            : "Auto-renewal is off. This subscription will not renew automatically.";
        string open = russian ? "Открыть подписку" : "View subscription";
        string html = $"""
            <!doctype html><html lang="{locale}"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1"></head>
            <body style="margin:0;padding:24px 12px;background:#f4f6fb;font-family:Arial,sans-serif;color:#172033">
            <table role="presentation" style="width:100%;max-width:600px;margin:0 auto;background:#fff;border-radius:12px" cellpadding="24"><tr><td>
            <p style="font-weight:bold">{WebUtility.HtmlEncode(brand)}</p><h1 style="font-size:24px">{subject}</h1>
            <p>{renewal}</p><p>{WebUtility.HtmlEncode(accessMessage)}</p><p>{thanks}</p><p>{feedback}</p>
            <p><a href="mailto:{FeedbackAddress}">{FeedbackAddress}</a></p>
            <p><a href="{WebUtility.HtmlEncode(subscriptionLink)}">{open}</a></p>
            </td></tr></table></body></html>
            """;
        string text = $"{renewal}\n{accessMessage}\n\n{thanks}\n{feedback}\n{FeedbackAddress}\n\n{open}: {subscriptionLink}";
        return new EmailTemplateContent(
            template is null ? subject : ApplyTokens(template.Subject, values, html: false),
            string.IsNullOrWhiteSpace(template?.HtmlBody) ? html : ApplyTokens(template.HtmlBody, values, html: true),
            string.IsNullOrWhiteSpace(template?.TextBody) ? text : ApplyTokens(template.TextBody, values, html: false));
    }

    private static string FormatEnd(DateTime instant, string? timeZoneId, string locale) {
        if (instant.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("A UTC access end is required.", nameof(instant));
        }
        TimeZoneInfo zone;
        try {
            zone = string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        } catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException) {
            zone = TimeZoneInfo.Utc;
        }
        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(instant, zone);
        TimeSpan offset = zone.GetUtcOffset(instant);
        string sign = offset < TimeSpan.Zero ? "-" : "+";
        string offsetText = offset.Duration().ToString(@"hh\:mm", CultureInfo.InvariantCulture);
        return local.ToString("g", CultureInfo.GetCultureInfo(locale)) + $" (UTC{sign}{offsetText})";
    }

    private static string BuildAccessMessage(QueueSubscriptionCancellationEmailCommand request, string locale) {
        if (request.AccessUntilUtc is not { } end) {
            return locale is "ru" ? "Доступ по этой подписке завершён." : "Access through this subscription has ended.";
        }
        string date = FormatEnd(end, request.TimeZoneId, locale);
        return locale is "ru" ? $"Доступ по подписке сохранится до {date}." : $"Your subscription access continues until {date}.";
    }

    private static string ApplyTokens(string value, IReadOnlyDictionary<string, string> values, bool html) =>
        Tokens().Replace(value, match => {
            if (!values.TryGetValue(match.Groups["token"].Value, out string? replacement)) {
                return match.Value;
            }
            return html ? WebUtility.HtmlEncode(replacement) : replacement;
        });

    [GeneratedRegex(@"\{\{(?<token>[a-z]+)\}\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Tokens();
}
