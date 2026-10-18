using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using FoodDiary.Modules.Export.Infrastructure.Services.DiaryPdf;

namespace FoodDiary.Modules.Export.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class DiaryPdfChartAxisTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleDayChart_AlignsDateTickWithDataMarker(bool multipleSeries) {
        XDocument chart = RenderSingleDay(multipleSeries);
        XElement label = Assert.Single(chart.Descendants(), node => string.Equals(node.Name.LocalName, "text", StringComparison.Ordinal)
            && string.Equals(node.Value, "12 Oct", StringComparison.Ordinal));
        XElement marker = Assert.Single(chart.Descendants(), node => string.Equals(node.Name.LocalName, "circle", StringComparison.Ordinal)
            && string.Equals((string?)node.Attribute(multipleSeries ? "fill" : "stroke"), "#123456", StringComparison.Ordinal)
            && !string.Equals((string?)node.Attribute("cy"), "14", StringComparison.Ordinal));

        double labelX = double.Parse(label.Attribute("x")!.Value, CultureInfo.InvariantCulture);
        double markerX = double.Parse(marker.Attribute("cx")!.Value, CultureInfo.InvariantCulture);
        Assert.Equal(markerX, labelX);
    }

    private static XDocument RenderSingleDay(bool multipleSeries) {
        Type renderer = typeof(DiaryPdfGenerator).GetNestedType("DiaryChartSvgRenderer", BindingFlags.NonPublic)!;
        MethodInfo render = renderer.GetMethod(multipleSeries ? "RenderMultiLineChart" : "RenderLineChart", BindingFlags.Public | BindingFlags.Static)!;
        object[] arguments;
        if (multipleSeries) {
            Type seriesType = typeof(DiaryPdfGenerator).GetNestedType("ChartSeries", BindingFlags.NonPublic)!;
            var series = Array.CreateInstance(seriesType, 1);
            series.SetValue(Activator.CreateInstance(seriesType, "Protein", new double[] { 25 }, "#123456"), 0);
            arguments = [new[] { "12 Oct" }, series];
        } else {
            arguments = [new[] { "12 Oct" }, new double[] { 175 }, "#123456", "#eeeeee"];
        }

        return XDocument.Parse(Assert.IsType<string>(render.Invoke(null, arguments)));
    }
}
