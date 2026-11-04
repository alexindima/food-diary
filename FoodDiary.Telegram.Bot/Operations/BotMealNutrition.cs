using System.Globalization;
using System.Text.Json.Serialization;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotMealNutrition(decimal Calories, decimal Protein, decimal Fat, decimal Carbs,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? ItemNames = null) {
    internal const int MaxListedItems = 8;
    internal const int MaxListedItemLength = 256;

    internal string Format(bool russian) {
        string totals = russian
            ? string.Create(CultureInfo.InvariantCulture, $"Оценка по фото: {Calories:0.#} ккал\nБелки: {Protein:0.#} г · Жиры: {Fat:0.#} г · Углеводы: {Carbs:0.#} г")
            : string.Create(CultureInfo.InvariantCulture, $"Photo estimate: {Calories:0.#} kcal\nProtein: {Protein:0.#} g · Fat: {Fat:0.#} g · Carbs: {Carbs:0.#} g");
        if (ItemNames is not { Count: > 0 }) {
            return totals;
        }
        string items = string.Join('\n', ItemNames.Take(MaxListedItems).Select(name => $"• {name}"));
        if (ItemNames.Count > MaxListedItems) {
            items += russian ? "\n… Остальные позиции в записи." : "\n… See the entry for the remaining items.";
        }
        return $"{(russian ? "Что распознано" : "Recognized foods")}:\n{items}\n\n{totals}";
    }
}
