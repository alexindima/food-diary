using System.Diagnostics;
using OpenTelemetry;

namespace FoodDiary.JobManager.Services;

internal sealed class TelemetryPrivacyProcessor : BaseProcessor<Activity> {
    private static readonly HashSet<string> SensitiveTags = new(StringComparer.Ordinal) {
        "db.query.text",
        "db.statement",
        "error.message",
        "exception.message",
        "exception.stacktrace",
        "url.query",
        "user.email",
        "user.id",
    };

    public override void OnEnd(Activity data) {
        if (data.GetTagItem("url.full") is string url) {
            data.SetTag("url.full", SanitizeUrl(url));
        }

        string[] tagsToRemove = [.. data.TagObjects
            .Where(static tag => IsSensitiveTag(tag.Key))
            .Select(static tag => tag.Key)];
        foreach (string tag in tagsToRemove) {
            data.SetTag(tag, value: null);
        }

        if (data.Status == ActivityStatusCode.Error && data.StatusDescription is not null) {
            data.SetStatus(ActivityStatusCode.Error);
        }
    }

    public static void EnrichClientActivity(Activity activity, HttpRequestMessage request) {
        activity.SetTag("url.full", SanitizeUrl(request.RequestUri));
    }

    public static void EnrichClientActivity(Activity activity, HttpResponseMessage response) {
        activity.SetTag("url.full", SanitizeUrl(response.RequestMessage?.RequestUri));
    }

    private static string? SanitizeUrl(Uri? uri) =>
        uri is { IsAbsoluteUri: true }
            ? uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped)
            : null;

    private static string? SanitizeUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            ? SanitizeUrl(uri)
            : null;

    private static bool IsSensitiveTag(string key) =>
        SensitiveTags.Contains(key) ||
        key.EndsWith(".body", StringComparison.Ordinal) ||
        key.Contains("payload", StringComparison.OrdinalIgnoreCase) ||
        key.StartsWith("db.query.parameter", StringComparison.Ordinal) ||
        key.StartsWith("gen_ai.input", StringComparison.Ordinal) ||
        key.StartsWith("gen_ai.output", StringComparison.Ordinal) ||
        key.StartsWith("http.request.header.", StringComparison.Ordinal) ||
        key.StartsWith("http.response.header.", StringComparison.Ordinal);
}
