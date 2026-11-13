using System.Text.Json;

namespace FoodDiary.Testing;

public static class JsonElementIndexers {
    extension(JsonElement element) {
        public JsonElement this[string propertyName] => element.GetProperty(propertyName);
    }
}
