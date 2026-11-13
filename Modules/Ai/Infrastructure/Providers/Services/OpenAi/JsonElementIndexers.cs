using System.Text.Json;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

internal static class JsonElementIndexers {
    extension(JsonElement element) {
        internal JsonElement this[string propertyName] => element.GetProperty(propertyName);
    }
}
