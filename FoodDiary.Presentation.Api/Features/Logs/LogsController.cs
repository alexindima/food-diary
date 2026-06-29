using FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Logs;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v{version:apiVersion}/logs")]
[SuppressRequestAccessLog]
public sealed class LogsController(
    ILogger<LogsController> logger,
    ISender sender) : ControllerBase {
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Create([FromBody] ClientTelemetryLogHttpRequest request) {
        await sender.Send(
            new RecordFastingTelemetryCommand(
                request.Category,
                request.Name,
                request.Timestamp,
                request.Details),
            HttpContext.RequestAborted);

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

        logger.Log(
            logLevel,
            "Client telemetry event {Category}/{Name}: {Message}. Outcome={Outcome}; StatusCode={StatusCode}; DurationMs={DurationMs}; Value={Value}; Unit={Unit}; Route={Route}; PageRoute={PageRoute}; SessionId={SessionId}; Location={Location}; Method={HttpMethod}; BuildVersion={BuildVersion}; Details={Details}; Stack={Stack}",
            request.Category,
            request.Name,
            request.Message ?? string.Empty,
            request.Outcome,
            request.StatusCode,
            request.DurationMs,
            request.Value,
            request.Unit,
            request.Route,
            request.PageRoute,
            request.SessionId,
            request.Location,
            request.HttpMethod,
            request.BuildVersion,
            details,
            request.Stack);

        return NoContent();
    }
}
