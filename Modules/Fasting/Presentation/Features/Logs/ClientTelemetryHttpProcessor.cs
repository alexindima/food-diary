using FoodDiary.Modules.Fasting.Application.Common;
using FoodDiary.Modules.Fasting.Presentation.Features.Logs.Requests;
using FoodDiary.Modules.Fasting.Presentation.Features.Logs.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoodDiary.Modules.Fasting.Presentation.Features.Logs;

public sealed class ClientTelemetryHttpProcessor(ISender sender, ILogger<LogsController> logger) {
    public async Task<Result> ProcessAsync(
        ClientTelemetryLogHttpRequest request,
        bool hasAuthenticatedUser,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Result result = hasAuthenticatedUser && IsFastingTelemetry(request)
            ? await sender.Send(request.ToFastingTelemetryCommand(), cancellationToken).ConfigureAwait(false)
            : Result.Success();
        LogLevel logLevel = request.Level.ToLowerInvariant() switch {
            "error" => LogLevel.Warning,
            "warning" => LogLevel.Warning,
            _ => LogLevel.Information,
        };

        string? pageRoute = NormalizeTelemetryRoute(request.PageRoute);
        string? metricId = GetWebVitalMetricId(request);
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal) {
            ["ClientTelemetryCategory"] = request.Category,
            ["ClientTelemetryName"] = request.Name,
            ["ClientTelemetryPageRoute"] = pageRoute,
            ["ClientTelemetryMetricId"] = metricId,
            ["ClientTelemetryHttpMethod"] = NormalizeForLog(request.HttpMethod, 16),
            ["ClientTelemetryStatusCode"] = request.StatusCode,
            ["ClientTelemetryOutcome"] = NormalizeForLog(request.Outcome, 32),
            ["ClientTelemetryDurationMs"] = request.DurationMs,
            ["ClientTelemetryValue"] = request.Value,
            ["ClientTelemetryUnit"] = NormalizeForLog(request.Unit, 16),
            ["ClientTelemetryBuildVersion"] = NormalizeForLog(request.BuildVersion, 64),
            ["ClientTelemetryTimestamp"] = request.Timestamp,
        });
        logger.Log(
            logLevel,
            "Client telemetry event {Category}/{Name}. Outcome={Outcome}; StatusCode={StatusCode}; DurationMs={DurationMs}; Value={Value}; Unit={Unit}; PageRoute={PageRoute}; Method={HttpMethod}; BuildVersion={BuildVersion}; MetricId={MetricId}",
            request.Category,
            request.Name,
            NormalizeForLog(request.Outcome, 32),
            request.StatusCode,
            request.DurationMs,
            request.Value,
            NormalizeForLog(request.Unit, 16),
            pageRoute,
            NormalizeForLog(request.HttpMethod, 16),
            NormalizeForLog(request.BuildVersion, 64),
            metricId);
        return result;
    }

    private static bool IsFastingTelemetry(ClientTelemetryLogHttpRequest request) =>
        string.Equals(request.Category, "user_action", StringComparison.Ordinal) &&
        FastingTelemetryEventNames.IsSupported(request.Name);

    private static string? GetWebVitalMetricId(ClientTelemetryLogHttpRequest request) {
        if (request.Category is not "web_vital" ||
            request.Details is not { ValueKind: JsonValueKind.Object } details ||
            !details.TryGetProperty("metricId", out JsonElement property) ||
            property.ValueKind is not JsonValueKind.String ||
            property.GetString() is not string value || value.Length > 48) {
            return null;
        }
        // Only the official anonymous web-vitals format is retained. Arbitrary
        // client details, session identifiers and user-supplied IDs stay out of logs.
        string[] parts = value.Split('-');
        return parts.Length == 3 && parts[0].Length > 1 && parts[0][0] == 'v' &&
            parts[0][1..].All(char.IsAsciiDigit) &&
            parts[1].Length == 13 && parts[1].All(char.IsAsciiDigit) &&
            parts[2].Length == 13 && parts[2].All(char.IsAsciiDigit)
            ? value : null;
    }

    private static string? NormalizeTelemetryRoute(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }
        int suffix = value.IndexOfAny(['?', '#']);
        string path = suffix >= 0 ? value[..suffix] : value;
        if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute) && absolute.Scheme is "http" or "https") {
            path = absolute.AbsolutePath;
        }
        if (!path.StartsWith('/')) {
            return "/unknown";
        }
        string[] segments = path.Split('/');
        for (int index = 0; index < segments.Length; index++) {
            int matrix = segments[index].IndexOf(';', StringComparison.Ordinal);
            string segment = matrix >= 0 ? segments[index][..matrix] : segments[index];
            string decoded;
            try {
                decoded = Uri.UnescapeDataString(segment);
            } catch (UriFormatException) {
                segments[index] = ":invalid";
                continue;
            }
            segments[index] = Guid.TryParse(decoded, out _) ||
                (decoded.Length > 0 && decoded.All(char.IsAsciiDigit)) || decoded.Contains('@', StringComparison.Ordinal)
                ? ":id" : segment;
        }
        return NormalizeForLog(string.Join('/', segments), 256);
    }

    private static string? NormalizeForLog(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = string.Concat(value
            .Take(maxLength)
            .Select(static character => char.IsControl(character) ? ' ' : character));
        return normalized.Trim();
    }
}
