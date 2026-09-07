using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OpenTelemetry;

namespace FoodDiary.MailInbox.Infrastructure.Extensions;

internal sealed class MailInboxTelemetryPrivacyProcessor : BaseProcessor<Activity> {
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

    public static bool ShouldCollectRequest(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

    public static void EnrichServerActivity(Activity activity, HttpResponse response) {
        HttpContext context = response.HttpContext;
        var endpoint = context.GetEndpoint() as RouteEndpoint;
        string route = endpoint?.RoutePattern.RawText ?? "unmatched";
        if (route.Length > 0 && route[0] != '/' && !string.Equals(route, "unmatched", StringComparison.Ordinal)) {
            route = "/" + route;
        }

        activity.DisplayName = $"{context.Request.Method} {route}";
        activity.SetTag("http.route", route);
        activity.SetTag("url.path", value: null);
        activity.SetTag("url.query", value: null);
    }

    private static bool IsSensitiveTag(string key) =>
        SensitiveTags.Contains(key) ||
        key.EndsWith(".body", StringComparison.Ordinal) ||
        key.Contains("payload", StringComparison.OrdinalIgnoreCase) ||
        key.StartsWith("db.query.parameter", StringComparison.Ordinal) ||
        key.StartsWith("http.request.header.", StringComparison.Ordinal) ||
        key.StartsWith("http.response.header.", StringComparison.Ordinal);
}
