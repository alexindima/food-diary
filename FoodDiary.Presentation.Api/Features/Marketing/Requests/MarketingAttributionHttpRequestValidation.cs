using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FoodDiary.Presentation.Api.Features.Marketing.Requests;

internal static class MarketingAttributionHttpRequestValidation {
    public static IReadOnlyList<ValidationResult> Validate(
        string? timestamp,
        string? anonymousId,
        string? sessionId,
        string? landingPath,
        string? referrerHost,
        string? utmSource,
        string? utmMedium,
        string? utmCampaign,
        string? utmContent,
        string? utmTerm,
        string? buildVersion) {
        List<ValidationResult> failures = [];
        ValidateRequired(timestamp, 64, nameof(MarketingAttributionHttpRequest.Timestamp), failures);
        ValidateRequired(anonymousId, 96, nameof(MarketingAttributionHttpRequest.AnonymousId), failures);
        ValidateRequired(sessionId, 96, nameof(MarketingAttributionHttpRequest.SessionId), failures);
        ValidateRequired(landingPath, 512, nameof(MarketingAttributionHttpRequest.LandingPath), failures);
        ValidateOptional(referrerHost, 128, nameof(MarketingAttributionHttpRequest.ReferrerHost), failures);
        ValidateOptional(utmSource, 160, nameof(MarketingAttributionHttpRequest.UtmSource), failures);
        ValidateOptional(utmMedium, 160, nameof(MarketingAttributionHttpRequest.UtmMedium), failures);
        ValidateOptional(utmCampaign, 160, nameof(MarketingAttributionHttpRequest.UtmCampaign), failures);
        ValidateOptional(utmContent, 160, nameof(MarketingAttributionHttpRequest.UtmContent), failures);
        ValidateOptional(utmTerm, 160, nameof(MarketingAttributionHttpRequest.UtmTerm), failures);
        ValidateOptional(buildVersion, 64, nameof(MarketingAttributionHttpRequest.BuildVersion), failures);
        return failures;
    }

    private static void ValidateRequired(
        string? value,
        int maxLength,
        string memberName,
        ICollection<ValidationResult> failures) {
        if (string.IsNullOrWhiteSpace(value)) {
            failures.Add(new ValidationResult("Value is required.", [memberName]));
            return;
        }

        ValidateOptional(value, maxLength, memberName, failures);
    }

    private static void ValidateOptional(
        string? value,
        int maxLength,
        string memberName,
        ICollection<ValidationResult> failures) {
        if (value?.Length > maxLength) {
            failures.Add(new ValidationResult(
                string.Create(CultureInfo.InvariantCulture, $"Value must be at most {maxLength} characters."),
                [memberName]));
        }

        if (value?.Any(char.IsControl) == true) {
            failures.Add(new ValidationResult("Control characters are not supported.", [memberName]));
        }
    }
}
