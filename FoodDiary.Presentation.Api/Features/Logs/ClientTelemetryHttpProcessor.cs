using FoodDiary.Application.Fasting.Common;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Features.Logs.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Logs;

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

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal) {
            ["ClientTelemetryCategory"] = request.Category,
            ["ClientTelemetryName"] = request.Name,
            ["ClientTelemetryPageRoute"] = NormalizeForLog(request.PageRoute, 256),
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
            "Client telemetry event {Category}/{Name}. Outcome={Outcome}; StatusCode={StatusCode}; DurationMs={DurationMs}; Value={Value}; Unit={Unit}; PageRoute={PageRoute}; Method={HttpMethod}; BuildVersion={BuildVersion}",
            request.Category,
            request.Name,
            NormalizeForLog(request.Outcome, 32),
            request.StatusCode,
            request.DurationMs,
            request.Value,
            NormalizeForLog(request.Unit, 16),
            NormalizeForLog(request.PageRoute, 256),
            NormalizeForLog(request.HttpMethod, 16),
            NormalizeForLog(request.BuildVersion, 64));
        return result;
    }

    private static bool IsFastingTelemetry(ClientTelemetryLogHttpRequest request) =>
        string.Equals(request.Category, "user_action", StringComparison.Ordinal) &&
        FastingTelemetryEventNames.IsSupported(request.Name);

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
