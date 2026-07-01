using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static partial class DiaryChartSvgRenderer {
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
                    double x = values.Count <= 1 ? left + (plotWidth / 2) : left + (plotWidth * index / (values.Count - 1));
                    double y = top + plotHeight - (plotHeight * Math.Clamp(value, 0, maxValue) / maxValue);
                    return new Point(x, y);
                })
                .ToArray();
        }

        private static string BuildSmoothPath(IReadOnlyList<Point> points) {
            switch (points.Count) {
                case 0:
                    return "";
                case 1:
                    return FormattableString.Invariant($"M {points[0].X} {points[0].Y}");
            }

            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"M {points[0].X} {points[0].Y}");

            for (int index = 0; index < points.Count - 1; index++) {
                Point current = points[index];
                Point next = points[index + 1];
                double controlOffset = (next.X - current.X) / 2;
                sb.Append(CultureInfo.InvariantCulture, $" C {current.X + controlOffset} {current.Y}, {next.X - controlOffset} {next.Y}, {next.X} {next.Y}");
            }

            return sb.ToString();
        }

        private static string BuildAreaPath(IReadOnlyList<Point> points, double baseline) {
            if (points.Count == 0) {
                return "";
            }

            string linePath = BuildSmoothPath(points);
            Point first = points[0];
            Point last = points[^1];
            return FormattableString.Invariant($"{linePath} L {last.X} {baseline} L {first.X} {baseline} Z");
        }

        private static double NiceMax(double value) {
            if (value <= 0) {
                return 1;
            }

            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
            double normalized = value / magnitude;
            int nice = normalized switch {
                <= 1 => 1,
                <= 2 => 2,
                <= 5 => 5,
                _ => 10,
            };

            return nice * magnitude;
        }

        private static string FormatAxis(double value) =>
            value >= 1000
                ? Math.Round(value, MidpointRounding.ToEven).ToString("N0", CultureInfo.InvariantCulture)
                : Math.Round(value, 1, MidpointRounding.ToEven).ToString("0.#", CultureInfo.InvariantCulture);

        private static string Escape(string value) => WebUtility.HtmlEncode(value);

        [StructLayout(LayoutKind.Auto)]
        private readonly record struct Point(double X, double Y);
    }
}
