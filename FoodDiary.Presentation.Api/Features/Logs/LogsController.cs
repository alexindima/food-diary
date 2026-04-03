using FoodDiary.Presentation.Api.Features.Logs.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Logs;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v{version:apiVersion}/logs")]
public sealed class LogsController(ILogger<LogsController> logger) : ControllerBase {
    private readonly ILogger<LogsController> _logger = logger;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Create([FromBody] ClientTelemetryLogHttpRequest request) {
        var details = request.Details?.ValueKind is null or System.Text.Json.JsonValueKind.Null
            ? null
            : request.Details.Value.GetRawText();
        var logLevel = request.Level?.ToLowerInvariant() switch {
            "error" => LogLevel.Error,
            "warning" => LogLevel.Warning,
            _ => LogLevel.Information,
        };

        using var scope = _logger.BeginScope(new Dictionary<string, object?> {
            ["ClientTelemetryCategory"] = request.Category,
            ["ClientTelemetryName"] = request.Name,
            ["ClientTelemetryRoute"] = request.Route,
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

        _logger.Log(
            logLevel,
            "Client telemetry event {Category}/{Name}: {Message}. Outcome={Outcome}; StatusCode={StatusCode}; DurationMs={DurationMs}; Value={Value}; Unit={Unit}; Route={Route}; Location={Location}; Method={HttpMethod}; BuildVersion={BuildVersion}; Details={Details}; Stack={Stack}",
            request.Category,
            request.Name,
            request.Message ?? string.Empty,
            request.Outcome,
            request.StatusCode,
            request.DurationMs,
            request.Value,
            request.Unit,
            request.Route,
            request.Location,
            request.HttpMethod,
            request.BuildVersion,
            details,
            request.Stack);

        return Task.FromResult<IActionResult>(NoContent());
    }
}
