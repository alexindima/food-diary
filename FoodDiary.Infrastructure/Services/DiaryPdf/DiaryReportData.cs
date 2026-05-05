using System.Globalization;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private sealed record DiaryReportData(
        IReadOnlyList<Meal> Meals,
        IReadOnlyList<DiaryDay> Days,
        string PeriodStartLabel,
        string PeriodEndLabel,
        TimeSpan DisplayOffset,
        string TimeZoneOffsetLabel,
        string ReportHost,
        IReadOnlyDictionary<MealId, byte[]> MealImages,
        bool UseCompactMealsMode,
        DiaryPdfReportTexts Texts,
        CultureInfo Culture) {
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
            IReadOnlyList<Meal> meals,
            DateTime dateFrom,
            DateTime dateTo,
            IReadOnlyDictionary<MealId, byte[]> mealImages,
            bool useCompactMealsMode,
            DiaryPdfReportTexts texts,
            int? timeZoneOffsetMinutes,
            string reportHost) {
            var normalizedFrom = EnsureUtc(dateFrom);
            var normalizedTo = EnsureUtc(dateTo);
            if (normalizedTo < normalizedFrom) {
                (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
            }

            var culture = ResolveCulture(texts.CultureName);
            var displayOffset = ResolveDisplayOffset(normalizedFrom, timeZoneOffsetMinutes);
            var days = BuildDays(meals, normalizedFrom, normalizedTo, displayOffset, culture);
            var orderedMeals = meals.OrderBy(meal => meal.Date).ToArray();
            return new DiaryReportData(
                orderedMeals,
                days,
                days.FirstOrDefault()?.Label ?? normalizedFrom.ToString("yyyy-MM-dd", culture),
                days.LastOrDefault()?.Label ?? normalizedTo.ToString("yyyy-MM-dd", culture),
                displayOffset,
                FormatTimeZoneOffset(displayOffset),
                reportHost,
                mealImages,
                useCompactMealsMode,
                texts,
                culture);
        }

        private static IReadOnlyList<DiaryDay> BuildDays(
            IReadOnlyList<Meal> meals,
            DateTime dateFrom,
            DateTime dateTo,
            TimeSpan displayOffset,
            CultureInfo culture) {
            var duration = dateTo - dateFrom;
            var dayCount = Math.Clamp((int)Math.Ceiling(duration.TotalDays), 1, 366);
            var result = new List<DiaryDay>(dayCount);

            for (var index = 0; index < dayCount; index++) {
                var start = dateFrom.AddDays(index);
                var end = index == dayCount - 1 ? dateTo : start.AddDays(1).AddTicks(-1);
                var bucketMeals = meals
                    .Where(meal => meal.Date >= start && meal.Date <= end)
                    .ToArray();

                var labelDate = start.Add(displayOffset).Date;
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

            var timeOfDay = dateFrom.TimeOfDay;
            return timeOfDay <= TimeSpan.FromHours(12)
                ? -timeOfDay
                : TimeSpan.FromDays(1) - timeOfDay;
        }

        private static string FormatTimeZoneOffset(TimeSpan offset) {
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var absolute = offset.Duration();
            return $"UTC{sign}{(int)absolute.TotalHours:00}:{absolute.Minutes:00}";
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
