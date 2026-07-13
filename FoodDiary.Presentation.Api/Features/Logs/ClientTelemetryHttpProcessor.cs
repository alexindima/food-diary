using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Results;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Logs;

public sealed class ClientTelemetryHttpProcessor(ILogger<LogsController> logger) {
    public async Task<Result> ProcessAsync(
        ClientTelemetryLogHttpRequest request,
        Task<Result> resultTask,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        Result result = await resultTask.ConfigureAwait(false);
        string? details = request.Details?.ValueKind is null or System.Text.Json.JsonValueKind.Null
            ? null
            : request.Details.Value.GetRawText();
        LogLevel logLevel = request.Level.ToLowerInvariant() switch {
            "error" => LogLevel.Error,
            "warning" => LogLevel.Warning,
            _ => LogLevel.Information,
        };

        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal) {
            ["ClientTelemetryCategory"] = request.Category,
            ["ClientTelemetryName"] = request.Name,
            ["ClientTelemetryRoute"] = request.Route,
            ["ClientTelemetryPageRoute"] = request.PageRoute,
            ["ClientTelemetrySessionId"] = request.SessionId,
            ["ClientTelemetryLocation"] = request.Location,
            ["ClientTelemetryHttpMethod"] = request.HttpMethod,
            ["ClientTelemetryStatusCode"] = request.StatusCode,
            ["ClientTelemetryOutcome"] = request.Outcome,
            ["ClientTelemetryDurationMs"] = request.DurationMs,
            ["ClientTelemetryValue"] = request.Value,
            ["ClientTelemetryUnit"] = request.Unit,
            ["ClientTelemetryBuildVersion"] = request.BuildVersion,
            ["ClientTelemetryTimestamp"] = request.Timestamp,
        });
        logger.Log(logLevel, "Client telemetry event {Category}/{Name}: {Message}. Outcome={Outcome}; StatusCode={StatusCode}; DurationMs={DurationMs}; Value={Value}; Unit={Unit}; Route={Route}; PageRoute={PageRoute}; SessionId={SessionId}; Location={Location}; Method={HttpMethod}; BuildVersion={BuildVersion}; Details={Details}; Stack={Stack}", request.Category, request.Name, request.Message ?? string.Empty, request.Outcome, request.StatusCode, request.DurationMs, request.Value, request.Unit, request.Route, request.PageRoute, request.SessionId, request.Location, request.HttpMethod, request.BuildVersion, details, request.Stack);
        return result;
    }
}
