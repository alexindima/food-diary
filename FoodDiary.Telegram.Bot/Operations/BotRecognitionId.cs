using System.Text.Json.Serialization;

namespace FoodDiary.Telegram.Bot.Operations;

[JsonConverter(typeof(BotGuidIdJsonConverterFactory))]
internal readonly record struct BotRecognitionId(Guid Value) {
    public override string ToString() => Value.ToString();
}
