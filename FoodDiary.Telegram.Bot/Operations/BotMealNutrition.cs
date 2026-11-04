using System.Globalization;
using System.Text.Json.Serialization;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotMealNutrition(decimal Calories, decimal Protein, decimal Fat, decimal Carbs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? ItemNames = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BotMealNutrition.Item>? Items = null) {
    internal const int MaxListedItems = 8;
    internal const int MaxListedItemLength = 256;

    internal string Format(bool russian) {
        string totals = russian
            ? string.Create(CultureInfo.InvariantCulture, $"Оценка по фото: {Calories:0.#} ккал\nБелки: {Protein:0.#} г · Жиры: {Fat:0.#} г · Углеводы: {Carbs:0.#} г")
            : string.Create(CultureInfo.InvariantCulture, $"Photo estimate: {Calories:0.#} kcal\nProtein: {Protein:0.#} g · Fat: {Fat:0.#} g · Carbs: {Carbs:0.#} g");
        IReadOnlyList<string>? lines = Items?.Select(item => item.Format(russian)).ToArray() ?? ItemNames;
        if (lines is not { Count: > 0 }) {
            return totals;
        }
        string items = string.Join('\n', lines.Take(MaxListedItems).Select(line => $"• {line}"));
        if (lines.Count > MaxListedItems) {
            items += russian ? "\n… Остальные позиции в записи." : "\n… See the entry for the remaining items.";
        }
        return $"{(russian ? "Что распознано" : "Recognized foods")}:\n{items}\n\n{totals}";
    }

    internal sealed record Item(string Name, string? NameLocal, double Amount, string? Unit, double Calories) {
        internal string Format(bool russian) {
            string name = russian ? NameLocal ?? Name : Name;
            string? unit = russian ? Unit?.ToLowerInvariant() switch {
                "g" => "г",
                "kg" => "кг",
                "ml" => "мл",
                "l" => "л",
                "pcs" => "шт.",
                _ => Unit,
            } : Unit;
            if (Amount > 0 && unit is not null) {
                name += string.Create(CultureInfo.InvariantCulture, $", {Amount:0.##} {unit}");
            }
            string calorieUnit = russian ? "ккал" : "kcal";
            return string.Create(CultureInfo.InvariantCulture, $"{name}, ~{Calories:0.#} {calorieUnit}");
        }
    }
}
