using System.Globalization;
using System.Net;
using System.Text;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
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
                  <rect width="{width}" height="{height}" fill="{PanelBackground}"/>
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
                  <path d="{areaPath}" fill="{fillColor}" opacity="0.55"/>
                  <path d="{linePath}" fill="none" stroke="{lineColor}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
                """);

            foreach (var point in points) {
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <circle cx="{point.X}" cy="{point.Y}" r="4" fill="{PanelBackground}" stroke="{lineColor}" stroke-width="3"/>
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
                  <rect width="{width}" height="{height}" fill="{PanelBackground}"/>
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
                  <path d="{{areaPath}}" fill="{{fillColor}}" opacity="0.55"/>
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
