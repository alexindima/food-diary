using System.Text.Json;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public static class NotificationPayloadSerializer {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize<TPayload>(TPayload payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static TPayload? Deserialize<TPayload>(string? payloadJson) {
        return string.IsNullOrWhiteSpace(payloadJson) ? default : JsonSerializer.Deserialize<TPayload>(payloadJson, JsonOptions);
    }

    public static bool TryDeserialize<TPayload>(string? payloadJson, out TPayload? payload) {
        payload = default;

        if (string.IsNullOrWhiteSpace(payloadJson)) {
            return false;
        }

        try {
            payload = JsonSerializer.Deserialize<TPayload>(payloadJson, JsonOptions);
            return payload is not null;
        } catch (JsonException) {
            return false;
        }
    }
}
