using System.Globalization;

namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotStatisticsFormatter {
    internal static string Format(BotDiaryStatistics summary, bool russian) {
        if (summary.Days is null || summary.CalendarDays is not (1 or 7) || summary.Days.Count != summary.CalendarDays) {
            throw new InvalidDataException("Invalid statistics period.");
        }
        var culture = CultureInfo.GetCultureInfo(russian ? "ru-RU" : "en-US");
        string Number(double value) => value.ToString("0.#", culture);
        string Date(DateOnly value) => value.ToString("dd.MM.yyyy", culture);
        var lines = new List<string>();
        if (summary.CalendarDays == 1) {
            lines.Add($"{(russian ? "Сегодня" : "Today")}: {Date(summary.Days[0].Date)}");
            string goal = summary.Days[0].CalorieGoal is { } target ? $" / {Number(target)}" : string.Empty;
            lines.Add($"{(russian ? "Калории" : "Calories")}: {Number(summary.TotalCalories)}{goal} {(russian ? "ккал" : "kcal")}");
        } else {
            lines.Add($"{Date(summary.Days[0].Date)} – {Date(summary.Days[^1].Date)}");
            lines.Add($"{(russian ? "Всего калорий" : "Total calories")}: {Number(summary.TotalCalories)} {(russian ? "ккал" : "kcal")}");
            lines.Add($"{(russian ? "В среднем за 7 календарных дней" : "Average over 7 calendar days")}: {Number(summary.AverageCaloriesPerCalendarDay)} {(russian ? "ккал/день" : "kcal/day")}");
            lines.Add($"{(russian ? "Дней с приёмами пищи" : "Days with meals")}: {summary.DaysWithMeals.ToString(culture)}/7");
        }
        lines.Add($"{(russian ? "Белки / жиры / углеводы" : "Protein / fat / carbs")}: {Number(summary.TotalProteins)} / {Number(summary.TotalFats)} / {Number(summary.TotalCarbs)} {(russian ? "г" : "g")}");
        string waterGoal = summary.CalendarDays == 1 && summary.DailyWaterGoalMl is { } water ? $" / {Number(water)}" : string.Empty;
        lines.Add($"{(russian ? "Вода" : "Water")}: {summary.TotalWaterMl.ToString(culture)}{waterGoal} {(russian ? "мл" : "ml")}");
        lines.Add($"{(russian ? "Приёмов пищи" : "Meals")}: {summary.MealCount.ToString(culture)}");
        if (summary.MealCount == 0 && summary.TotalWaterMl == 0) {
            lines.Add(russian ? "За этот период записей еды и воды нет." : "No food or water entries in this period.");
        }
        lines.Add($"{(russian ? "Часовой пояс" : "Time zone")}: {summary.TimeZoneId}");
        return string.Join('\n', lines);
    }
}
