using System.Globalization;
using System.Text;
using FoodDiary.Application.Abstractions.Meals.Models;

namespace FoodDiary.Application.Export.Services;

public static class DiaryCsvGenerator {
    public static byte[] Generate(IReadOnlyList<MealConsumptionReadModel> meals, int? timeZoneOffsetMinutes = null) =>
        Generate(meals, ResolveDisplayOffset(timeZoneOffsetMinutes));

    public static byte[] Generate(IReadOnlyList<MealConsumptionReadModel> meals, TimeSpan displayOffset) {
        var sb = new StringBuilder();
        sb.AppendLine("Date,MealType,Calories,Proteins,Fats,Carbs,Fiber,Alcohol,Comment");

        foreach (MealConsumptionReadModel meal in meals) {
            double calories = meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;
            double proteins = meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;
            double fats = meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;
            double carbs = meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;
            double fiber = meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;
            double alcohol = meal.IsNutritionAutoCalculated ? meal.TotalAlcohol : meal.ManualAlcohol ?? meal.TotalAlcohol;

            sb.Append(ToDisplayDate(meal.Date, displayOffset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(meal.MealType?.ToString() ?? "");
            sb.Append(',');
            sb.Append(Math.Round(calories, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(proteins, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(fats, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(carbs, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(fiber, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Math.Round(alcohol, 1, MidpointRounding.ToEven).ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.AppendLine(EscapeCsv(meal.Comment));
        }

        byte[] preamble = Encoding.UTF8.GetPreamble();
        byte[] content = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] result = new byte[preamble.Length + content.Length];
        preamble.CopyTo(result, 0);
        content.CopyTo(result, preamble.Length);
        return result;
    }

    private static string EscapeCsv(string? value) {
        if (string.IsNullOrEmpty(value)) {
            return "";
        }

        ReadOnlySpan<char> valueSpan = value.AsSpan();
        if (valueSpan.Contains('"') ||
            valueSpan.Contains(',') ||
            valueSpan.Contains('\n') ||
            valueSpan.Contains('\r')) {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static TimeSpan ResolveDisplayOffset(int? timeZoneOffsetMinutes) =>
        timeZoneOffsetMinutes is >= -840 and <= 840
            ? TimeSpan.FromMinutes(timeZoneOffsetMinutes.Value)
            : TimeSpan.Zero;

    private static DateTime ToDisplayDate(DateTime value, TimeSpan displayOffset) {
        DateTime utc = value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return utc.Add(displayOffset);
    }
}
