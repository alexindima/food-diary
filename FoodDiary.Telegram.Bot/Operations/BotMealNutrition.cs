using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotMealNutrition(decimal Calories, decimal Protein, decimal Fat, decimal Carbs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? ItemNames = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BotMealNutrition.Item>? Items = null) {
    internal const int MaxListedItems = 8;
    internal const int MaxListedItemLength = 256;

    internal string Format(bool russian) {
        var culture = CultureInfo.GetCultureInfo(russian ? "ru-RU" : "en-US");
        string totals = russian
            ? string.Create(culture, $"🔥 <b>~{Calories:0.#} ккал</b>\nБ {Protein:0.#} г · Ж {Fat:0.#} г · У {Carbs:0.#} г")
            : string.Create(culture, $"🔥 <b>~{Calories:0.#} kcal</b>\nP {Protein:0.#} g · F {Fat:0.#} g · C {Carbs:0.#} g");
        IReadOnlyList<string>? lines = Items?.Select(item => item.FormatHtml(russian, culture)).ToArray()
            ?? ItemNames?.Select(name => WebUtility.HtmlEncode(name) ?? string.Empty).ToArray();
        if (lines is not { Count: > 0 }) {
            return totals;
        }
        string items = string.Join('\n', lines.Take(MaxListedItems).Select(line => $"• {line}"));
        if (lines.Count > MaxListedItems) {
            items += russian ? "\n… Остальные позиции в записи." : "\n… See the entry for the remaining items.";
        }
        return $"{totals}\n<b>{(russian ? "Распознано" : "Recognized foods")}:</b>\n{items}";
    }

    internal sealed record Item(string Name, string? NameLocal, double Amount, string? Unit, double Calories) {
        internal string FormatHtml(bool russian, CultureInfo culture) {
            string name = WebUtility.HtmlEncode(russian ? NameLocal ?? Name : Name);
            string? unit = russian ? Unit?.ToLowerInvariant() switch {
                "g" => "г",
                "kg" => "кг",
                "ml" => "мл",
                "l" => "л",
                "pcs" => "шт.",
                _ => Unit,
            } : Unit;
            if (Amount > 0 && unit is not null) {
                name += string.Create(culture, $" · {Amount:0.##} {WebUtility.HtmlEncode(unit)}");
            }
            string calorieUnit = russian ? "ккал" : "kcal";
            return string.Create(culture, $"{name} · ~{Calories:0.#} {calorieUnit}");
        }
    }
}
