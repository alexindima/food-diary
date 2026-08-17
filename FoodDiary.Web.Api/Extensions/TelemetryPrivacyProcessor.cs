using System.Diagnostics;
using OpenTelemetry;

namespace FoodDiary.Web.Api.Extensions;

public sealed class TelemetryPrivacyProcessor : BaseProcessor<Activity> {
    public const string UnmatchedRouteLabel = "unmatched";

    private static readonly HashSet<string> SensitiveTags = new(StringComparer.Ordinal) {
        "client.address",
        "client.port",
        "db.query.text",
        "db.statement",
        "enduser.id",
        "error.message",
        "exception.message",
        "exception.stacktrace",
        "network.peer.address",
        "network.peer.port",
        "url.query",
        "user.email",
        "user.id",
        "user_agent.original",
    };

    public override void OnEnd(Activity data) {
        Sanitize(data);
    }

    public static bool ShouldCollectRequest(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

    public static void EnrichServerActivity(Activity activity, HttpResponse response) {
        HttpContext context = response.HttpContext;
        string route = ResolveRouteLabel(context);
        activity.DisplayName = $"{context.Request.Method} {route}";
        activity.SetTag("http.route", route);
        activity.SetTag("url.path", value: null);
        activity.SetTag("url.query", value: null);
    }

    public static void EnrichClientActivity(Activity activity, HttpRequestMessage request) {
        activity.SetTag("url.full", SanitizeUrl(request.RequestUri));
    }

    public static void EnrichClientActivity(Activity activity, HttpResponseMessage response) {
        activity.SetTag("url.full", SanitizeUrl(response.RequestMessage?.RequestUri));
    }

    public static string ResolveRouteLabel(HttpContext context) {
        var routeEndpoint = context.GetEndpoint() as RouteEndpoint;
        string? routePattern = routeEndpoint?.RoutePattern.RawText;
        if (string.IsNullOrWhiteSpace(routePattern)) {
            return UnmatchedRouteLabel;
        }

        return routePattern[0] == '/'
            ? routePattern
            : "/" + routePattern;
    }

    public static void Sanitize(Activity activity) {
        if (activity.GetTagItem("url.full") is string url) {
            activity.SetTag("url.full", SanitizeUrl(url));
        }

        string[] tagsToRemove = [.. activity.TagObjects
            .Where(static tag => IsSensitiveTag(tag.Key))
            .Select(static tag => tag.Key)];
        foreach (string tag in tagsToRemove) {
            activity.SetTag(tag, value: null);
        }

        if (activity.Status == ActivityStatusCode.Error && activity.StatusDescription is not null) {
            activity.SetStatus(ActivityStatusCode.Error);
        }
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
