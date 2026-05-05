using System.Globalization;
using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Domain.Entities.Meals;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services;

internal sealed class DiaryPdfGenerator : IDiaryPdfGenerator {
    private const string PageBackground = "#11161d";
    private const string PanelBackground = "#22272f";
    private const string CardBackground = "#262c34";
    private const string BorderColor = "#3a424e";
    private const string GridColor = "#3d4652";
    private const string TextColor = "#f5f7fb";
    private const string MutedTextColor = "#aebbd0";
    private const string PrimaryColor = "#63e6be";
    private const string PrimaryFillColor = "#244d45";
    private const string ProteinColor = "#3b82f6";
    private const string FatColor = "#f5d76e";
    private const string CarbColor = "#00b894";
    private const string FiberColor = "#7c3aed";

    public byte[] Generate(IReadOnlyList<Meal> meals, DateTime dateFrom, DateTime dateTo) {
        QuestPDF.Settings.License = LicenseType.Community;

        var report = DiaryReportData.Create(meals, dateFrom, dateTo);

        var document = Document.Create(container => {
            container.Page(page => {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.PageColor(PageBackground);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextColor));

                page.Header().Element(c => ComposeHeader(c, report));
                page.Content().Element(c => ComposeContent(c, report));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, DiaryReportData report) {
        container.PaddingBottom(12).Row(row => {
            row.RelativeItem().Column(column => {
                column.Item().Text("Food Diary Report")
                    .FontSize(18).Bold().FontColor(TextColor);

                column.Item().Text(text => {
                    text.Span("Period: ").SemiBold().FontColor(MutedTextColor);
                    text.Span($"{report.PeriodStartLabel} - {report.PeriodEndLabel}");
                    text.Span("  |  ").FontColor(BorderColor);
                    text.Span($"{report.MealCount} meals").FontColor(MutedTextColor);
                });
            });

            row.ConstantItem(150).AlignRight().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture))
                .FontSize(8).FontColor(MutedTextColor);
        });
    }

    private static void ComposeContent(IContainer container, DiaryReportData report) {
        container.Column(column => {
            column.Spacing(10);
            column.Item().Element(c => ComposeSummarySection(c, report));
            column.Item().PageBreak();
            column.Item().Element(c => ComposeNutritionChartSection(c, report));
            column.Item().PageBreak();
            column.Item().Element(c => ComposeMealsTable(c, report));
        });
    }

    private static void ComposeSummarySection(IContainer container, DiaryReportData report) {
        container.Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(12).Column(column => {
            column.Spacing(12);
            column.Item().Text("Period summary").FontSize(13).SemiBold().FontColor(MutedTextColor);

            column.Item().Row(row => {
                row.Spacing(8);

                row.RelativeItem(2).Element(c => ComposeTotalCaloriesCard(c, report));
                row.RelativeItem().Element(c => ComposeAverageCard(c, report));
            });

            column.Item().Row(row => {
                row.Spacing(8);

                row.RelativeItem().Element(c => ComposeMacroCard(c, "Proteins", report.AverageProteins, ProteinColor, report.ProteinSeries));
                row.RelativeItem().Element(c => ComposeMacroCard(c, "Fats", report.AverageFats, FatColor, report.FatSeries));
                row.RelativeItem().Element(c => ComposeMacroCard(c, "Carbs", report.AverageCarbs, CarbColor, report.CarbSeries));
                row.RelativeItem().Element(c => ComposeMacroCard(c, "Fiber", report.AverageFiber, FiberColor, report.FiberSeries));
            });
        });
    }

    private static void ComposeTotalCaloriesCard(IContainer container, DiaryReportData report) {
        container.Background(CardBackground).Border(1).BorderColor(BorderColor).Padding(12).Height(180).Column(column => {
            column.Item().Text("Total calories").FontSize(9).SemiBold().FontColor(MutedTextColor);
            column.Item().Text($"{FormatNumber(report.TotalCalories, 0)} kcal").FontSize(26).Bold().FontColor(TextColor);
            column.Item().ExtendVertical().AlignBottom().Svg(DiaryChartSvgRenderer.RenderWideSparkline(report.CalorieSeries, PrimaryColor, PrimaryFillColor)).FitArea();
        });
    }

    private static void ComposeAverageCard(IContainer container, DiaryReportData report) {
        container.Background(CardBackground).Border(1).BorderColor(BorderColor).Padding(12).Height(180).Column(column => {
            column.Item().Text("Average per day").FontSize(9).SemiBold().FontColor(MutedTextColor);
            column.Item().Text($"{FormatNumber(report.AverageCalories, 0)} kcal").FontSize(26).Bold().FontColor(TextColor);
            column.Item().PaddingTop(14).LineHorizontal(1).LineColor(BorderColor);
            column.Item().PaddingTop(12).Row(row => {
                row.RelativeItem().Column(metric => {
                    metric.Item().Text(FormatNumber(report.TotalProteins, 1)).FontSize(18).Bold();
                    metric.Item().Text("g proteins").FontSize(8).FontColor(MutedTextColor);
                });
                row.RelativeItem().Column(metric => {
                    metric.Item().Text(FormatNumber(report.TotalCarbs, 1)).FontSize(18).Bold();
                    metric.Item().Text("g carbs").FontSize(8).FontColor(MutedTextColor);
                });
            });
        });
    }

    private static void ComposeMacroCard(IContainer container, string title, double value, string color, IReadOnlyList<double> series) {
        container.Background(CardBackground).BorderTop(3).BorderColor(color).Padding(8).Height(92).Column(column => {
            column.Item().Text(title).FontSize(8).SemiBold().FontColor(MutedTextColor);
            column.Item().Text($"{FormatNumber(value, 1)} g").FontSize(20).Bold().FontColor(TextColor);
            column.Item().ExtendVertical().AlignBottom().Svg(DiaryChartSvgRenderer.RenderSparkline(series, color, ApplyAlpha(color, 0.24))).FitArea();
        });
    }

    private static void ComposeNutritionChartSection(IContainer container, DiaryReportData report) {
        container.Column(column => {
            column.Spacing(12);
            column.Item().Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(10).Column(chart => {
                chart.Spacing(8);
                chart.Item().Text("Calories by day").FontSize(12).SemiBold().FontColor(MutedTextColor);
                chart.Item().Height(150).Svg(DiaryChartSvgRenderer.RenderLineChart(
                    report.DayLabels,
                    report.CalorieSeries,
                    PrimaryColor,
                    PrimaryFillColor)).FitArea();
            });
            column.Item().Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(10).Column(chart => {
                chart.Spacing(8);
                chart.Item().Text("Nutrients by day").FontSize(12).SemiBold().FontColor(MutedTextColor);
                chart.Item().Height(150).Svg(DiaryChartSvgRenderer.RenderMultiLineChart(
                    report.DayLabels,
                    [
                        new ChartSeries("Proteins", report.ProteinSeries, ProteinColor),
                        new ChartSeries("Fats", report.FatSeries, FatColor),
                        new ChartSeries("Carbs", report.CarbSeries, CarbColor),
                        new ChartSeries("Fiber", report.FiberSeries, FiberColor),
                    ])).FitArea();
            });
        });
    }

    private static void ComposeMealsTable(IContainer container, DiaryReportData report) {
        var meals = report.Meals;

        container.Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(12).Column(column => {
            column.Spacing(8);
            column.Item().Text("Meals").FontSize(12).SemiBold().FontColor(MutedTextColor);

            if (meals.Count == 0) {
                column.Item().PaddingVertical(20).AlignCenter()
                    .Text("No meals recorded in this period.")
                    .FontSize(12).FontColor(MutedTextColor);
                return;
            }

            column.Item().Table(table => {
                table.ColumnsDefinition(columns => {
                    columns.RelativeColumn(1.7f);
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(1.2f);
                    columns.RelativeColumn(3);
                });

                HeaderCell(table, "Date");
                HeaderCell(table, "Meal type");
                HeaderCell(table, "Calories");
                HeaderCell(table, "Proteins");
                HeaderCell(table, "Fats");
                HeaderCell(table, "Carbs");
                HeaderCell(table, "Fiber");
                HeaderCell(table, "Comment");

                for (var i = 0; i < meals.Count; i++) {
                    var meal = meals[i];
                    var bg = i % 2 == 0 ? "#1c222a" : "#202832";

                    DataCell(table, report.FormatMealDate(meal.Date), bg);
                    DataCell(table, meal.MealType?.ToString() ?? "-", bg);
                    DataCell(table, FormatNumber(EffectiveCalories(meal), 1), bg);
                    DataCell(table, FormatNumber(EffectiveProteins(meal), 1), bg);
                    DataCell(table, FormatNumber(EffectiveFats(meal), 1), bg);
                    DataCell(table, FormatNumber(EffectiveCarbs(meal), 1), bg);
                    DataCell(table, FormatNumber(EffectiveFiber(meal), 1), bg);
                    DataCell(table, meal.Comment ?? "", bg);
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container) {
        container.AlignCenter().Text(text => {
            text.Span("Generated by Food Diary - ").FontSize(7).FontColor(MutedTextColor);
            text.Span("fooddiary.club").FontSize(7).FontColor(PrimaryColor);
        });
    }

    private static void HeaderCell(TableDescriptor table, string text) {
        table.Cell().Background("#17202b").BorderBottom(1).BorderColor(BorderColor).Padding(5)
            .Text(text).FontColor(MutedTextColor).SemiBold().FontSize(8);
    }

    private static void DataCell(TableDescriptor table, string text, string bg) {
        table.Cell().Background(bg).BorderBottom(1).BorderColor(BorderColor)
            .Padding(4).Text(text).FontSize(7).FontColor(TextColor);
    }

    private static double EffectiveCalories(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;

    private static double EffectiveProteins(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;

    private static double EffectiveFats(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;

    private static double EffectiveCarbs(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;

    private static double EffectiveFiber(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;

    private static string FormatNumber(double value, int decimals) =>
        Math.Round(value, decimals).ToString($"N{decimals}", CultureInfo.InvariantCulture);

    private static string ApplyAlpha(string hex, double alpha) {
        var normalized = hex.TrimStart('#');
        if (normalized.Length != 6) {
            return hex;
        }

        var alphaByte = (byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255);
        return $"#{alphaByte:X2}{normalized}";
    }

    private sealed record DiaryReportData(
        IReadOnlyList<Meal> Meals,
        IReadOnlyList<DiaryDay> Days,
        string PeriodStartLabel,
        string PeriodEndLabel,
        TimeSpan DisplayOffset) {
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
            EnsureUtc(date).Add(DisplayOffset).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        public static DiaryReportData Create(IReadOnlyList<Meal> meals, DateTime dateFrom, DateTime dateTo) {
            var normalizedFrom = EnsureUtc(dateFrom);
            var normalizedTo = EnsureUtc(dateTo);
            if (normalizedTo < normalizedFrom) {
                (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
            }

            var displayOffset = InferDisplayOffset(normalizedFrom);
            var days = BuildDays(meals, normalizedFrom, normalizedTo, displayOffset);
            var orderedMeals = meals.OrderBy(meal => meal.Date).ToArray();
            return new DiaryReportData(
                orderedMeals,
                days,
                days.FirstOrDefault()?.Label ?? normalizedFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                days.LastOrDefault()?.Label ?? normalizedTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                displayOffset);
        }

        private static IReadOnlyList<DiaryDay> BuildDays(
            IReadOnlyList<Meal> meals,
            DateTime dateFrom,
            DateTime dateTo,
            TimeSpan displayOffset) {
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
                    labelDate.ToString("d MMM", CultureInfo.InvariantCulture),
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

        private static TimeSpan InferDisplayOffset(DateTime dateFrom) {
            var timeOfDay = dateFrom.TimeOfDay;
            return timeOfDay <= TimeSpan.FromHours(12)
                ? -timeOfDay
                : TimeSpan.FromDays(1) - timeOfDay;
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

    private static class DiaryChartSvgRenderer {
        public static string RenderLineChart(IReadOnlyList<string> labels, IReadOnlyList<double> values, string lineColor, string fillColor) {
            const double width = 2200;
            const double height = 360;
            const double left = 52;
            const double right = 16;
            const double top = 18;
            const double bottom = 34;

            var plotWidth = width - left - right;
            var plotHeight = height - top - bottom;
            var maxValue = NiceMax(values.DefaultIfEmpty(0).Max());
            var points = BuildPoints(values, left, top, plotWidth, plotHeight, maxValue);
            var linePath = BuildSmoothPath(points);
            var areaPath = BuildAreaPath(points, top + plotHeight);
            var sb = new StringBuilder();

            sb.Append(CultureInfo.InvariantCulture, $"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}">
                  <rect width="{width}" height="{height}" fill="#22272f"/>
                """);

            for (var tick = 0; tick <= 4; tick++) {
                var y = top + plotHeight - plotHeight * tick / 4;
                var value = maxValue * tick / 4;
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{left}" y1="{y}" x2="{width - right}" y2="{y}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{left - 10}" y="{y + 4}" text-anchor="end" fill="{MutedTextColor}" font-size="11" font-family="Arial">{FormatAxis(value)}</text>
                """);
            }

            var labelStep = Math.Max(1, (int)Math.Ceiling(labels.Count / 8d));
            for (var index = 0; index < labels.Count; index += labelStep) {
                var x = labels.Count <= 1 ? left : left + plotWidth * index / (labels.Count - 1);
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{x}" y1="{top}" x2="{x}" y2="{top + plotHeight}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{x}" y="{height - 10}" text-anchor="middle" fill="{MutedTextColor}" font-size="11" font-family="Arial">{Escape(labels[index])}</text>
                """);
            }

            sb.Append(CultureInfo.InvariantCulture, $"""
                  <path d="{areaPath}" fill="{fillColor}" opacity="0.78"/>
                  <path d="{linePath}" fill="none" stroke="{lineColor}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
                """);

            foreach (var point in points) {
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <circle cx="{point.X}" cy="{point.Y}" r="4" fill="#22272f" stroke="{lineColor}" stroke-width="3"/>
                """);
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        public static string RenderMultiLineChart(IReadOnlyList<string> labels, IReadOnlyList<ChartSeries> series) {
            const double width = 2200;
            const double height = 360;
            const double left = 52;
            const double right = 16;
            const double top = 32;
            const double bottom = 34;

            var plotWidth = width - left - right;
            var plotHeight = height - top - bottom;
            var maxValue = NiceMax(series.SelectMany(item => item.Values).DefaultIfEmpty(0).Max());
            var sb = new StringBuilder();

            sb.Append(CultureInfo.InvariantCulture, $"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}">
                  <rect width="{width}" height="{height}" fill="#22272f"/>
                """);

            for (var tick = 0; tick <= 4; tick++) {
                var y = top + plotHeight - plotHeight * tick / 4;
                var value = maxValue * tick / 4;
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{left}" y1="{y}" x2="{width - right}" y2="{y}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{left - 10}" y="{y + 4}" text-anchor="end" fill="{MutedTextColor}" font-size="11" font-family="Arial">{FormatAxis(value)}</text>
                """);
            }

            var labelStep = Math.Max(1, (int)Math.Ceiling(labels.Count / 8d));
            for (var index = 0; index < labels.Count; index += labelStep) {
                var x = labels.Count <= 1 ? left : left + plotWidth * index / (labels.Count - 1);
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{x}" y1="{top}" x2="{x}" y2="{top + plotHeight}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{x}" y="{height - 10}" text-anchor="middle" fill="{MutedTextColor}" font-size="11" font-family="Arial">{Escape(labels[index])}</text>
                """);
            }

            for (var index = 0; index < series.Count; index++) {
                var item = series[index];
                var points = BuildPoints(item.Values, left, top, plotWidth, plotHeight, maxValue);
                var linePath = BuildSmoothPath(points);
                var legendX = left + index * 155;

                sb.Append(CultureInfo.InvariantCulture, $"""
                    <circle cx="{legendX}" cy="14" r="5" fill="{item.Color}"/>
                    <text x="{legendX + 12}" y="18" fill="{MutedTextColor}" font-size="12" font-family="Arial">{Escape(item.Label)}</text>
                    <path d="{linePath}" fill="none" stroke="{item.Color}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
                """);
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        public static string RenderSparkline(IReadOnlyList<double> values, string lineColor, string fillColor) {
            const double width = 900;
            const double height = 180;
            const double padding = 8;

            return RenderSparkline(values, lineColor, fillColor, width, height, padding);
        }

        public static string RenderWideSparkline(IReadOnlyList<double> values, string lineColor, string fillColor) {
            const double width = 900;
            const double height = 180;
            const double padding = 8;

            return RenderSparkline(values, lineColor, fillColor, width, height, padding);
        }

        private static string RenderSparkline(
            IReadOnlyList<double> values,
            string lineColor,
            string fillColor,
            double width,
            double height,
            double padding) {
            var maxValue = Math.Max(1, values.DefaultIfEmpty(0).Max());
            var points = BuildPoints(values, padding, padding, width - padding * 2, height - padding * 2, maxValue);
            var linePath = BuildSmoothPath(points);
            var areaPath = BuildAreaPath(points, height - padding);

            return $$"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {{width.ToString(CultureInfo.InvariantCulture)}} {{height.ToString(CultureInfo.InvariantCulture)}}" preserveAspectRatio="none">
                  <path d="{{areaPath}}" fill="{{fillColor}}" opacity="0.8"/>
                  <path d="{{linePath}}" fill="none" stroke="{{lineColor}}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                """;
        }

        private static IReadOnlyList<Point> BuildPoints(
            IReadOnlyList<double> values,
            double left,
            double top,
            double plotWidth,
            double plotHeight,
            double maxValue) {
            if (values.Count == 0) {
                return [new Point(left, top + plotHeight)];
            }

            return values
                .Select((value, index) => {
                    var x = values.Count <= 1 ? left + plotWidth / 2 : left + plotWidth * index / (values.Count - 1);
                    var y = top + plotHeight - plotHeight * Math.Clamp(value, 0, maxValue) / maxValue;
                    return new Point(x, y);
                })
                .ToArray();
        }

        private static string BuildSmoothPath(IReadOnlyList<Point> points) {
            if (points.Count == 0) {
                return "";
            }

            if (points.Count == 1) {
                return FormattableString.Invariant($"M {points[0].X} {points[0].Y}");
            }

            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"M {points[0].X} {points[0].Y}");

            for (var index = 0; index < points.Count - 1; index++) {
                var current = points[index];
                var next = points[index + 1];
                var controlOffset = (next.X - current.X) / 2;
                sb.Append(CultureInfo.InvariantCulture, $" C {current.X + controlOffset} {current.Y}, {next.X - controlOffset} {next.Y}, {next.X} {next.Y}");
            }

            return sb.ToString();
        }

        private static string BuildAreaPath(IReadOnlyList<Point> points, double baseline) {
            if (points.Count == 0) {
                return "";
            }

            var linePath = BuildSmoothPath(points);
            var first = points[0];
            var last = points[^1];
            return FormattableString.Invariant($"{linePath} L {last.X} {baseline} L {first.X} {baseline} Z");
        }

        private static double NiceMax(double value) {
            if (value <= 0) {
                return 1;
            }

            var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            var normalized = value / magnitude;
            var nice = normalized <= 1
                ? 1
                : normalized <= 2
                    ? 2
                    : normalized <= 5
                        ? 5
                        : 10;

            return nice * magnitude;
        }

        private static string FormatAxis(double value) =>
            value >= 1000
                ? Math.Round(value).ToString("N0", CultureInfo.InvariantCulture)
                : Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);

        private static string Escape(string value) => WebUtility.HtmlEncode(value);

        private readonly record struct Point(double X, double Y);
    }
}
