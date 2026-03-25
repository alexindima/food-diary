using System.Text.Json.Serialization;

namespace FoodDiary.Presentation.Api.Responses;

public sealed record ApiErrorHttpResponse(
    string Error,
    string Message,
    string? TraceId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyDictionary<string, string[]>? Errors = null);
