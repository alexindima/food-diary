using System.Text.Json.Serialization;

namespace FoodDiary.MailInbox.Presentation.Responses;

public sealed record MailInboxApiErrorHttpResponse(
    string Error,
    string Message,
    string? TraceId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
