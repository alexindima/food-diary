using System.Globalization;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed record BotMealNutrition(decimal Calories, decimal Protein, decimal Fat, decimal Carbs) {
    internal string Format(bool russian) => russian
        ? string.Create(CultureInfo.InvariantCulture, $"Оценка по фото: {Calories:0.#} ккал\nБелки: {Protein:0.#} г · Жиры: {Fat:0.#} г · Углеводы: {Carbs:0.#} г")
        : string.Create(CultureInfo.InvariantCulture, $"Photo estimate: {Calories:0.#} kcal\nProtein: {Protein:0.#} g · Fat: {Fat:0.#} g · Carbs: {Carbs:0.#} g");
}
