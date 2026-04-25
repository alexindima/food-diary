using System.Text.Json.Serialization;

namespace FoodDiary.MailRelay.Presentation.Responses;

public sealed record MailRelayApiErrorHttpResponse(
    string Error,
    string Message,
    string? TraceId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
