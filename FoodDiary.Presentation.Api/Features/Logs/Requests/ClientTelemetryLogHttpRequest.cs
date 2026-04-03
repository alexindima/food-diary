using System.Text.Json;

namespace FoodDiary.Presentation.Api.Features.Logs.Requests;

public sealed record ClientTelemetryLogHttpRequest(
    string Category,
    string Name,
    string Level,
    string Timestamp,
    string? Message = null,
    string? Location = null,
    string? Route = null,
    string? HttpMethod = null,
    string? Outcome = null,
    double? DurationMs = null,
    double? Value = null,
    int? StatusCode = null,
    string? Unit = null,
    string? BuildVersion = null,
    string? Stack = null,
    JsonElement? Details = null);
