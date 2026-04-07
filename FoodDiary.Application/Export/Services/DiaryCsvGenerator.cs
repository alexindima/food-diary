using System.Globalization;
using System.Text;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Export.Services;

public static class DiaryCsvGenerator {
    public static byte[] Generate(IReadOnlyList<Meal> meals) {
        var sb = new StringBuilder();
        sb.AppendLine("Date,MealType,Calories,Proteins,Fats,Carbs,Fiber,Alcohol,Comment");

        foreach (var meal in meals) {
            var calories = meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;
            var proteins = meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;
            var fats = meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;
            var carbs = meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;
            var fiber = meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;
            var alcohol = meal.IsNutritionAutoCalculated ? meal.TotalAlcohol : meal.ManualAlcohol ?? meal.TotalAlcohol;

            sb.Append(meal.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(meal.MealType?.ToString() ?? "");
            sb.Append(',');
            sb.Append(Math.Round(calories, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(proteins, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(fats, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(carbs, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(fiber, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(alcohol, 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.AppendLine(EscapeCsv(meal.Comment));
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + content.Length];
        preamble.CopyTo(result, 0);
        content.CopyTo(result, preamble.Length);
        return result;
    }

    private static string EscapeCsv(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return "";
        }

        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')) {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
