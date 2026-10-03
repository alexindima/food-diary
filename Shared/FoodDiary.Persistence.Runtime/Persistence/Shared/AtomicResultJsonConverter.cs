using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Results;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class AtomicResultJsonConverter : JsonConverterFactory {
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Result)
        || (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>));

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        typeToConvert == typeof(Result) ? new NonGenericConverter()
            : (JsonConverter)Activator.CreateInstance(typeof(ValueConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()))!;

    private sealed class ValueConverter<T> : JsonConverter<Result<T>> {
        public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.GetProperty("success").GetBoolean()
                ? Result.Success(document.RootElement.GetProperty("value").Deserialize<T>(options)!)
                : Result.Failure<T>(document.RootElement.GetProperty("error").Deserialize<Error>(options)!);
        }

        public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteBoolean("success", value.IsSuccess);
            writer.WritePropertyName(value.IsSuccess ? "value" : "error");
            if (value.IsSuccess) {
                JsonSerializer.Serialize(writer, value.Value, options);
            } else {
                JsonSerializer.Serialize(writer, value.Error, options);
            }
            writer.WriteEndObject();
        }
    }

    private sealed class NonGenericConverter : JsonConverter<Result> {
        public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.GetProperty("success").GetBoolean() ? Result.Success()
                : Result.Failure(document.RootElement.GetProperty("error").Deserialize<Error>(options)!);
        }

        public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteBoolean("success", value.IsSuccess);
            if (value.IsFailure) {
                writer.WritePropertyName("error");
                JsonSerializer.Serialize(writer, value.Error, options);
            }
            writer.WriteEndObject();
        }
    }
}
