using System.Globalization;
using System.Net;

namespace FoodDiary.Telegram.Bot.Operations;

internal static class BotStatisticsFormatter {
    internal static string Format(BotDiaryStatistics summary, bool russian) {
        if (summary.Days is null || summary.CalendarDays is not (1 or 7) || summary.Days.Count != summary.CalendarDays) {
            throw new InvalidDataException("Invalid statistics period.");
        }
        var culture = CultureInfo.GetCultureInfo(russian ? "ru-RU" : "en-US");
        string Number(double value) => value.ToString("#,0.#", culture);
        string Date(DateOnly value) => value.ToString(russian ? "d MMMM" : "MMMM d", culture);

        bool today = summary.CalendarDays == 1;
        BotDiaryStatisticsDay firstDay = summary.Days[0];
        string date = Date(firstDay.Date);
        string period = russian ? "Сегодня" : "Today";
        if (!today) {
            date += $" – {Date(summary.Days[^1].Date)}";
            period = russian ? "7 дней" : "7 days";
        }
        string unit = russian ? "ккал" : "kcal";
        string calories = Number(summary.TotalCalories);
        var lines = new List<string> {
            $"📊 <b>{period} · {date}</b> ({WebUtility.HtmlEncode(summary.TimeZoneId)})",
        };
        if (today && firstDay.CalorieGoal is > 0 and { } target) {
            lines.Add($"🔥 <b>{calories} / {Number(target)} {unit}</b>");
            string percent = (summary.TotalCalories / target).ToString("0%", culture);
            double remaining = target - summary.TotalCalories;
            string remainingLabel = russian ? "Осталось" : "Remaining";
            string overGoalLabel = russian ? "Сверх цели" : "Over goal";
            lines.Add($"{percent} · {(remaining < 0 ? overGoalLabel : remainingLabel)} {Number(Math.Abs(remaining))} {unit}");
        } else {
            lines.Add($"🔥 <b>{calories} {unit}</b>");
        }
        if (!today) {
            string average = Number(summary.AverageCaloriesPerCalendarDay);
            string daysWithMeals = summary.DaysWithMeals.ToString(culture);
            string days = summary.CalendarDays.ToString(culture);
            lines.Add(russian ? $"В среднем {average} ккал/день" : $"Average {average} kcal/day");
            lines.Add(russian ? $"Дней с приёмами пищи: {daysWithMeals} из {days}" : $"Days with meals: {daysWithMeals} of {days}");
        }
        lines.Add(string.Empty);
        lines.AddRange(russian
            ? [
                "<b>Нутриенты</b>",
                $"🥩 Белки: {Number(summary.TotalProteins)} г",
                $"🥑 Жиры: {Number(summary.TotalFats)} г",
                $"🍞 Углеводы: {Number(summary.TotalCarbs)} г",
            ]
            : [
                "<b>Nutrients</b>",
                $"🥩 Protein: {Number(summary.TotalProteins)} g",
                $"🥑 Fat: {Number(summary.TotalFats)} g",
                $"🍞 Carbs: {Number(summary.TotalCarbs)} g",
            ]);
        lines.Add(string.Empty);
        string waterGoal = today && summary.DailyWaterGoalMl is { } water ? $" / {Number(water)}" : string.Empty;
        lines.Add($"💧 {(russian ? "Вода" : "Water")}: {summary.TotalWaterMl.ToString("#,0", culture)}{waterGoal} {(russian ? "мл" : "ml")}");
        lines.Add($"🍽️ {(russian ? "Приёмов пищи" : "Meals")}: {summary.MealCount.ToString(culture)}");
        if (summary.MealCount == 0 && summary.TotalWaterMl == 0) {
            lines.Add(russian ? "За этот период записей еды и воды нет." : "No food or water entries in this period.");
        }
        return string.Join('\n', lines);
    }
}
