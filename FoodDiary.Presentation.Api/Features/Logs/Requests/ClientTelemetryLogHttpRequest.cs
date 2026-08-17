using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using FoodDiary.Application.Fasting.Common;

namespace FoodDiary.Presentation.Api.Features.Logs.Requests;

public sealed record ClientTelemetryLogHttpRequest(
    string Category,
    string Name,
    string Level,
    string Timestamp,
    string? Message = null,
    string? Location = null,
    string? Route = null,
    string? PageRoute = null,
    string? SessionId = null,
    string? HttpMethod = null,
    string? Outcome = null,
    double? DurationMs = null,
    double? Value = null,
    int? StatusCode = null,
    string? Unit = null,
    string? BuildVersion = null,
    string? Stack = null,
    JsonElement? Details = null) : IValidatableObject {
    private const int DetailsMaxLength = 4096;
    private const int DetailsMaxDepth = 4;
    private const int DetailsMaxNodeCount = 64;
    private const int DetailsMaxPropertyNameLength = 64;
    private const int DetailsMaxStringLength = 512;
    private const int DetailsMaxArrayLength = 16;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        List<ValidationResult> failures = [];
        ValidateRequired(Category, 32, nameof(Category), failures);
        ValidateRequired(Name, 64, nameof(Name), failures);
        ValidateRequired(Level, 16, nameof(Level), failures);
        ValidateRequired(Timestamp, 64, nameof(Timestamp), failures);
        ValidateOptional(Message, 512, nameof(Message), failures);
        ValidateOptional(Location, 512, nameof(Location), failures);
        ValidateOptional(Route, 256, nameof(Route), failures);
        ValidateOptional(PageRoute, 256, nameof(PageRoute), failures);
        ValidateOptional(SessionId, 96, nameof(SessionId), failures);
        ValidateOptional(HttpMethod, 16, nameof(HttpMethod), failures);
        ValidateOptional(Outcome, 32, nameof(Outcome), failures);
        ValidateOptional(Unit, 16, nameof(Unit), failures);
        ValidateOptional(BuildVersion, 64, nameof(BuildVersion), failures);
        ValidateOptional(Stack, 1024, nameof(Stack), failures);

        if (!IsSupportedEvent(Category, Name)) {
            failures.Add(new ValidationResult(
                "Telemetry category and event name are not supported.",
                [nameof(Category), nameof(Name)]));
        }

        if (Level is not ("info" or "warning" or "error")) {
            failures.Add(new ValidationResult("Telemetry level is not supported.", [nameof(Level)]));
        }

        if (!DateTimeOffset.TryParse(
            Timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out _)) {
            failures.Add(new ValidationResult("Telemetry timestamp must be a valid date and time.", [nameof(Timestamp)]));
        }

        if (DurationMs is { } durationMs && (!double.IsFinite(durationMs) || durationMs is < 0 or > 600_000)) {
            failures.Add(new ValidationResult("Telemetry duration is outside the supported range.", [nameof(DurationMs)]));
        }

        if (Value is { } value && !double.IsFinite(value)) {
            failures.Add(new ValidationResult("Telemetry value must be finite.", [nameof(Value)]));
        }

        if (StatusCode is { } statusCode && statusCode is < 0 or > 599) {
            failures.Add(new ValidationResult("Telemetry status code is outside the supported range.", [nameof(StatusCode)]));
        }

        if (Details is { } details && !IsValidDetails(details)) {
            failures.Add(new ValidationResult("Telemetry details exceed the supported size or complexity.", [nameof(Details)]));
        }

        return failures;
    }

    private static bool IsSupportedEvent(string? category, string? name) {
        return category switch {
            "client_error" => name is "global-error",
            "http_request" => name is "api.request",
            "route_timing" => name is "router.navigation",
            "web_vital" => name is "ttfb" or "fcp" or "lcp",
            "user_action" => name is
                "notifications.settings.viewed" or
                "notifications.preference.changed" or
                "notifications.subscription.ensure" or
                "notifications.subscription.remove" or
                "notifications.test-push.schedule" || FastingTelemetryEventNames.IsSupported(name),
            _ => false,
        };
    }

    private static bool IsValidDetails(JsonElement details) {
        if (details.ValueKind is JsonValueKind.Undefined || details.GetRawText().Length > DetailsMaxLength) {
            return false;
        }

        int nodeCount = 0;
        return IsValidDetailsNode(details, depth: 0, ref nodeCount);
    }

    private static bool IsValidDetailsNode(JsonElement element, int depth, ref int nodeCount) {
        nodeCount++;
        if (depth > DetailsMaxDepth || nodeCount > DetailsMaxNodeCount) {
            return false;
        }

        switch (element.ValueKind) {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject()) {
                    if (property.Name.Length > DetailsMaxPropertyNameLength ||
                        !IsValidDetailsNode(property.Value, depth + 1, ref nodeCount)) {
                        return false;
                    }
                }

                return true;
            case JsonValueKind.Array:
                JsonElement.ArrayEnumerator items = element.EnumerateArray();
                int itemCount = 0;
                foreach (JsonElement item in items) {
                    itemCount++;
                    if (itemCount > DetailsMaxArrayLength || !IsValidDetailsNode(item, depth + 1, ref nodeCount)) {
                        return false;
                    }
                }

                return true;
            case JsonValueKind.String:
                return element.GetString()?.Length <= DetailsMaxStringLength;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true;
            default:
                return false;
        }
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
    }
}
