using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodDiary.Telegram.Bot.Operations;

// Typed identities retain the original GUID JSON scalars in durable checkpoints.
internal sealed class BotGuidIdJsonConverterFactory : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(BotOperationId) || typeToConvert == typeof(BotImageAssetId) ||
        typeToConvert == typeof(BotRecognitionId) || typeToConvert == typeof(BotMealId) ||
        typeToConvert == typeof(BotHydrationEntryId);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
        if (typeToConvert == typeof(BotOperationId)) {
            return new GuidIdConverter<BotOperationId>(value => new(value), value => value.Value);
        }
        if (typeToConvert == typeof(BotImageAssetId)) {
            return new GuidIdConverter<BotImageAssetId>(value => new(value), value => value.Value);
        }
        if (typeToConvert == typeof(BotRecognitionId)) {
            return new GuidIdConverter<BotRecognitionId>(value => new(value), value => value.Value);
        }
        if (typeToConvert == typeof(BotMealId)) {
            return new GuidIdConverter<BotMealId>(value => new(value), value => value.Value);
        }
        if (typeToConvert == typeof(BotHydrationEntryId)) {
            return new GuidIdConverter<BotHydrationEntryId>(value => new(value), value => value.Value);
        }
        throw new NotSupportedException($"Unsupported bot identity type: {typeToConvert.Name}.");
    }

    private sealed class GuidIdConverter<TId>(Func<Guid, TId> create, Func<TId, Guid> unwrap) : JsonConverter<TId> {
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            create(JsonSerializer.Deserialize<Guid>(ref reader, options));

        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, unwrap(value), options);
    }
}
