using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Content;

public sealed class EmailTemplate : Entity<Guid> {
    private const int KeyMaxLength = 64;
    private const int LocaleMaxLength = 8;
    private const int SubjectMaxLength = 256;

    public string Key { get; private set; } = string.Empty;
    public string Locale { get; private set; } = "en";
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string TextBody { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private EmailTemplate() {
    }

    public static EmailTemplate Create(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive) {
        string normalizedKey = NormalizeKey(key);
        string normalizedLocale = NormalizeLocale(locale);
        string normalizedSubject = NormalizeRequired(subject, nameof(subject), SubjectMaxLength);
        string normalizedHtmlBody = NormalizeRequired(htmlBody, nameof(htmlBody));
        string normalizedTextBody = NormalizeRequired(textBody, nameof(textBody));

        var template = new EmailTemplate {
            Id = Guid.NewGuid(),
            Key = normalizedKey,
            Locale = normalizedLocale,
            Subject = normalizedSubject,
            HtmlBody = normalizedHtmlBody,
            TextBody = normalizedTextBody,
            IsActive = isActive
        };
        template.SetCreated();
        return template;
    }

    public void Update(
        string subject,
        string htmlBody,
        string textBody,
        bool isActive) {
        string normalizedSubject = NormalizeRequired(subject, nameof(subject), SubjectMaxLength);
        string normalizedHtmlBody = NormalizeRequired(htmlBody, nameof(htmlBody));
        string normalizedTextBody = NormalizeRequired(textBody, nameof(textBody));

        if (string.Equals(Subject, normalizedSubject, StringComparison.Ordinal) &&
            string.Equals(HtmlBody, normalizedHtmlBody, StringComparison.Ordinal) &&
            string.Equals(TextBody, normalizedTextBody, StringComparison.Ordinal) &&
            IsActive == isActive) {
            return;
        }

        Subject = normalizedSubject;
        HtmlBody = normalizedHtmlBody;
        TextBody = normalizedTextBody;
        IsActive = isActive;
        SetModified();
    }

    private static string NormalizeKey(string value) {
        return NormalizeRequired(value, nameof(value), KeyMaxLength).ToLowerInvariant();
    }

    private static string NormalizeLocale(string value) {
        string preferred = LanguageCode.FromPreferred(value).Value;
        return preferred.Length > LocaleMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Value must be at most {LocaleMaxLength} characters.")
            : preferred;
    }

    private static string NormalizeRequired(string value, string paramName, int? maxLength = null) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", paramName);
        }

        string normalized = value.Trim();
        if (maxLength.HasValue && normalized.Length > maxLength.Value) {
            throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength.Value} characters.");
        }

        return normalized;
    }
}
