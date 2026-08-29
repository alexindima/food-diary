using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using FoodDiary.Modules.Fasting.Application.Common;

namespace FoodDiary.Presentation.Api.Features.Logs.Requests;

internal static class ClientTelemetryLogHttpRequestValidation {
    private const int DetailsMaxLength = 4096;
    private const int DetailsMaxDepth = 4;
    private const int DetailsMaxNodeCount = 64;
    private const int DetailsMaxPropertyNameLength = 64;
    private const int DetailsMaxStringLength = 512;
    private const int DetailsMaxArrayLength = 16;
    private static readonly TimeSpan MaxFutureTimestampSkew = TimeSpan.FromMinutes(5);

    public static IReadOnlyList<ValidationResult> Validate(
        ClientTelemetryLogHttpRequest request,
        DateTimeOffset utcNow) {
        List<ValidationResult> failures = [];
        ValidateRequired(request.Category, 32, nameof(request.Category), failures);
        ValidateRequired(request.Name, 64, nameof(request.Name), failures);
        ValidateRequired(request.Level, 16, nameof(request.Level), failures);
        ValidateRequired(request.Timestamp, 64, nameof(request.Timestamp), failures);
        ValidateOptional(request.Message, 512, nameof(request.Message), failures);
        ValidateOptional(request.Location, 512, nameof(request.Location), failures);
        ValidateOptional(request.Route, 256, nameof(request.Route), failures);
        ValidateOptional(request.PageRoute, 256, nameof(request.PageRoute), failures);
        ValidateOptional(request.SessionId, 96, nameof(request.SessionId), failures);
        ValidateOptional(request.HttpMethod, 16, nameof(request.HttpMethod), failures);
        ValidateOptional(request.Outcome, 32, nameof(request.Outcome), failures);
        ValidateOptional(request.Unit, 16, nameof(request.Unit), failures);
        ValidateOptional(request.BuildVersion, 64, nameof(request.BuildVersion), failures);
        ValidateOptional(request.Stack, 1024, nameof(request.Stack), failures);

        if (!IsSupportedEvent(request.Category, request.Name)) {
            failures.Add(new ValidationResult(
                "Telemetry category and event name are not supported.",
                [nameof(request.Category), nameof(request.Name)]));
        }

        if (request.Level is not ("info" or "warning" or "error")) {
            failures.Add(new ValidationResult("Telemetry level is not supported.", [nameof(request.Level)]));
        }

        if (!DateTimeOffset.TryParse(
            request.Timestamp,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset timestamp)) {
            failures.Add(new ValidationResult("Telemetry timestamp must be a valid date and time.", [nameof(request.Timestamp)]));
        } else if (timestamp > utcNow.Add(MaxFutureTimestampSkew)) {
            failures.Add(new ValidationResult("Telemetry timestamp is too far in the future.", [nameof(request.Timestamp)]));
        }

        if (request.DurationMs is { } durationMs && (!double.IsFinite(durationMs) || durationMs is < 0 or > 600_000)) {
            failures.Add(new ValidationResult("Telemetry duration is outside the supported range.", [nameof(request.DurationMs)]));
        }

        if (request.Value is { } value && !double.IsFinite(value)) {
            failures.Add(new ValidationResult("Telemetry value must be finite.", [nameof(request.Value)]));
        }

        if (request.StatusCode is { } statusCode && statusCode is < 0 or > 599) {
            failures.Add(new ValidationResult("Telemetry status code is outside the supported range.", [nameof(request.StatusCode)]));
        }

        if (request.Details is { } details && !IsValidDetails(details)) {
            failures.Add(new ValidationResult("Telemetry details exceed the supported size or complexity.", [nameof(request.Details)]));
        }

        return failures;
    }

    private static bool IsSupportedEvent(string? category, string? name) =>
        category switch {
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
