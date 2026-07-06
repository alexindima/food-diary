using System.Globalization;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private sealed record DiaryReportData(
        IReadOnlyList<MealConsumptionReadModel> Meals,
        IReadOnlyList<DiaryDay> Days,
        string PeriodStartLabel,
        string PeriodEndLabel,
        TimeSpan DisplayOffset,
        string TimeZoneOffsetLabel,
        string ReportHost,
        IReadOnlyDictionary<Guid, byte[]> MealImages,
        bool UseCompactMealsMode,
        DiaryPdfReportTexts Texts,
        CultureInfo Culture,
        DateTime GeneratedAtUtc) {
        public int MealCount => Meals.Count;
        public int DayCount => Math.Max(1, Days.Count);
        public double TotalCalories => Days.Sum(day => day.Calories);
        public double TotalProteins => Days.Sum(day => day.Proteins);
        public double TotalFats => Days.Sum(day => day.Fats);
        public double TotalCarbs => Days.Sum(day => day.Carbs);
        public double TotalFiber => Days.Sum(day => day.Fiber);
        public double AverageCalories => TotalCalories / DayCount;
        public double AverageProteins => TotalProteins / DayCount;
        public double AverageFats => TotalFats / DayCount;
        public double AverageCarbs => TotalCarbs / DayCount;
        public double AverageFiber => TotalFiber / DayCount;
        public IReadOnlyList<string> DayLabels => Days.Select(day => day.Label).ToArray();
        public IReadOnlyList<double> CalorieSeries => Days.Select(day => day.Calories).ToArray();
        public IReadOnlyList<double> ProteinSeries => Days.Select(day => day.Proteins).ToArray();
        public IReadOnlyList<double> FatSeries => Days.Select(day => day.Fats).ToArray();
        public IReadOnlyList<double> CarbSeries => Days.Select(day => day.Carbs).ToArray();
        public IReadOnlyList<double> FiberSeries => Days.Select(day => day.Fiber).ToArray();

        public string FormatMealDate(DateTime date) =>
            EnsureUtc(date).Add(DisplayOffset).ToString("yyyy-MM-dd HH:mm", Culture);

        public string FormatMealType(MealType? mealType) =>
            mealType switch {
                MealType.Breakfast => Texts.BreakfastMealType,
                MealType.Lunch => Texts.LunchMealType,
                MealType.Dinner => Texts.DinnerMealType,
                MealType.Snack => Texts.SnackMealType,
                _ => Texts.OtherMealType,
            };

        public static DiaryReportData Create(
            IReadOnlyList<MealConsumptionReadModel> meals,
            DateTime dateFrom,
            DateTime dateTo,
            IReadOnlyDictionary<Guid, byte[]> mealImages,
            bool useCompactMealsMode,
            DiaryPdfReportTexts texts,
            int? timeZoneOffsetMinutes,
            string reportHost,
            DateTime generatedAtUtc) {
            DateTime normalizedFrom = EnsureUtc(dateFrom);
            DateTime normalizedTo = EnsureUtc(dateTo);
            if (normalizedTo < normalizedFrom) {
                (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
            }

            CultureInfo culture = ResolveCulture(texts.CultureName);
            TimeSpan displayOffset = ResolveDisplayOffset(normalizedFrom, timeZoneOffsetMinutes);
            IReadOnlyList<DiaryDay> days = BuildDays(meals, normalizedFrom, normalizedTo, displayOffset, culture);
            MealConsumptionReadModel[] orderedMeals = [.. meals.OrderBy(meal => meal.Date)];
            string firstDayLabel = days.Count > 0
                ? days[0].Label
                : normalizedFrom.ToString("yyyy-MM-dd", culture);
            string lastDayLabel = days.Count > 0
                ? days[^1].Label
                : normalizedTo.ToString("yyyy-MM-dd", culture);
            return new DiaryReportData(
                orderedMeals,
                days,
                firstDayLabel,
                lastDayLabel,
                displayOffset,
                FormatTimeZoneOffset(displayOffset),
                reportHost,
                mealImages,
                useCompactMealsMode,
                texts,
                culture,
                EnsureUtc(generatedAtUtc));
        }

        private static IReadOnlyList<DiaryDay> BuildDays(
            IReadOnlyList<MealConsumptionReadModel> meals,
            DateTime dateFrom,
            DateTime dateTo,
            TimeSpan displayOffset,
            CultureInfo culture) {
            TimeSpan duration = dateTo - dateFrom;
            int dayCount = Math.Clamp((int)Math.Ceiling(duration.TotalDays), 1, 366);
            var result = new List<DiaryDay>(dayCount);

            for (int index = 0; index < dayCount; index++) {
                DateTime start = dateFrom.AddDays(index);
                DateTime end = index == dayCount - 1 ? dateTo : start.AddDays(1).AddTicks(-1);
                MealConsumptionReadModel[] bucketMeals = [.. meals.Where(meal => meal.Date >= start && meal.Date <= end)];

                DateTime labelDate = start.Add(displayOffset).Date;
                result.Add(new DiaryDay(
                    labelDate.ToString("d MMM", culture),
                    bucketMeals.Sum(EffectiveCalories),
                    bucketMeals.Sum(EffectiveProteins),
                    bucketMeals.Sum(EffectiveFats),
                    bucketMeals.Sum(EffectiveCarbs),
                    bucketMeals.Sum(EffectiveFiber)));
            }

            return result;
        }

        private static DateTime EnsureUtc(DateTime value) =>
            value.Kind switch {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };

        private static CultureInfo ResolveCulture(string? cultureName) {
            try {
                return string.IsNullOrWhiteSpace(cultureName)
                    ? CultureInfo.GetCultureInfo("en")
                    : CultureInfo.GetCultureInfo(cultureName);
            } catch (CultureNotFoundException) {
                return CultureInfo.GetCultureInfo("en");
            }
        }

        private static TimeSpan ResolveDisplayOffset(DateTime dateFrom, int? timeZoneOffsetMinutes) {
            if (timeZoneOffsetMinutes is >= -840 and <= 840) {
                return TimeSpan.FromMinutes(timeZoneOffsetMinutes.Value);
            }

            TimeSpan timeOfDay = dateFrom.TimeOfDay;
            return timeOfDay <= TimeSpan.FromHours(12)
                ? -timeOfDay
                : TimeSpan.FromDays(1) - timeOfDay;
        }

        private static string FormatTimeZoneOffset(TimeSpan offset) {
            string sign = offset < TimeSpan.Zero ? "-" : "+";
            TimeSpan absolute = offset.Duration();
            return string.Create(CultureInfo.InvariantCulture, $"UTC{sign}{(int)absolute.TotalHours:00}:{absolute.Minutes:00}");
        }
    }

    private sealed record DiaryDay(
        string Label,
        double Calories,
        double Proteins,
        double Fats,
        double Carbs,
        double Fiber);

    private sealed record ChartSeries(
        string Label,
        IReadOnlyList<double> Values,
        string Color);
}
