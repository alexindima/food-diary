using System.Text.Json;

namespace FoodDiary.Application.Notifications.Common;

public static class NotificationPayloadSerializer {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize<TPayload>(TPayload payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);

    public static TPayload? Deserialize<TPayload>(string? payloadJson) {
        if (string.IsNullOrWhiteSpace(payloadJson)) {
            return default;
        }

        return JsonSerializer.Deserialize<TPayload>(payloadJson, JsonOptions);
    }
}
