using System.Globalization;
using System.Text;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static partial class DiaryChartSvgRenderer {
        public static string RenderLineChart(IReadOnlyList<string> labels, IReadOnlyList<double> values, string lineColor, string fillColor) {
            const double width = 2200;
            const double height = 360;
            const double left = 52;
            const double right = 16;
            const double top = 18;
            const double bottom = 34;

            const double plotWidth = width - left - right;
            const double plotHeight = height - top - bottom;
            double maxValue = NiceMax(values.DefaultIfEmpty(0).Max());
            IReadOnlyList<Point> points = BuildPoints(values, left, top, plotWidth, plotHeight, maxValue);
            string linePath = BuildSmoothPath(points);
            string areaPath = BuildAreaPath(points, top + plotHeight);
            var sb = new StringBuilder();

            sb.Append(CultureInfo.InvariantCulture, $"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}">
                  <rect width="{width}" height="{height}" fill="{PanelBackground}"/>
                """);

            for (int tick = 0; tick <= 4; tick++) {
                double y = top + plotHeight - (plotHeight * tick / 4);
                double value = maxValue * tick / 4;
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{left}" y1="{y}" x2="{width - right}" y2="{y}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{left - 10}" y="{y + 4}" text-anchor="end" fill="{MutedTextColor}" font-size="11" font-family="Arial">{FormatAxis(value)}</text>
                """);
            }

            int labelStep = Math.Max(1, (int)Math.Ceiling(labels.Count / 8d));
            for (int index = 0; index < labels.Count; index += labelStep) {
                double x = labels.Count <= 1 ? left : left + (plotWidth * index / (labels.Count - 1));
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{x}" y1="{top}" x2="{x}" y2="{top + plotHeight}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{x}" y="{height - 10}" text-anchor="middle" fill="{MutedTextColor}" font-size="11" font-family="Arial">{Escape(labels[index])}</text>
                """);
            }

            sb.Append(CultureInfo.InvariantCulture, $"""
                  <path d="{areaPath}" fill="{fillColor}" opacity="0.55"/>
                  <path d="{linePath}" fill="none" stroke="{lineColor}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
                """);

            foreach (Point point in points) {
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

            const double plotWidth = width - left - right;
            const double plotHeight = height - top - bottom;
            double maxValue = NiceMax(series.SelectMany(item => item.Values).DefaultIfEmpty(0).Max());
            var sb = new StringBuilder();

            sb.Append(CultureInfo.InvariantCulture, $"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}">
                  <rect width="{width}" height="{height}" fill="{PanelBackground}"/>
                """);

            for (int tick = 0; tick <= 4; tick++) {
                double y = top + plotHeight - (plotHeight * tick / 4);
                double value = maxValue * tick / 4;
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{left}" y1="{y}" x2="{width - right}" y2="{y}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{left - 10}" y="{y + 4}" text-anchor="end" fill="{MutedTextColor}" font-size="11" font-family="Arial">{FormatAxis(value)}</text>
                """);
            }

            int labelStep = Math.Max(1, (int)Math.Ceiling(labels.Count / 8d));
            for (int index = 0; index < labels.Count; index += labelStep) {
                double x = labels.Count <= 1 ? left : left + (plotWidth * index / (labels.Count - 1));
                sb.Append(CultureInfo.InvariantCulture, $"""
                    <line x1="{x}" y1="{top}" x2="{x}" y2="{top + plotHeight}" stroke="{GridColor}" stroke-width="1"/>
                    <text x="{x}" y="{height - 10}" text-anchor="middle" fill="{MutedTextColor}" font-size="11" font-family="Arial">{Escape(labels[index])}</text>
                """);
            }

            for (int index = 0; index < series.Count; index++) {
                ChartSeries item = series[index];
                IReadOnlyList<Point> points = BuildPoints(item.Values, left, top, plotWidth, plotHeight, maxValue);
                string linePath = BuildSmoothPath(points);
                double legendX = left + (index * 155);

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
            double maxValue = Math.Max(1, values.DefaultIfEmpty(0).Max());
            IReadOnlyList<Point> points = BuildPoints(values, padding, padding, width - (padding * 2), height - (padding * 2), maxValue);
            string linePath = BuildSmoothPath(points);
            string areaPath = BuildAreaPath(points, height - padding);

            return $$"""
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {{width.ToString(CultureInfo.InvariantCulture)}} {{height.ToString(CultureInfo.InvariantCulture)}}" preserveAspectRatio="none">
                  <path d="{{areaPath}}" fill="{{fillColor}}" opacity="0.55"/>
                  <path d="{{linePath}}" fill="none" stroke="{{lineColor}}" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
                """;
        }

    }
}
