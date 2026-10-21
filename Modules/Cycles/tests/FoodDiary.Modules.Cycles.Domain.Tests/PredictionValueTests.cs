using FoodDiary.Modules.Cycles.Domain.ValueObjects;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class PredictionValueTests {
    [Fact]
    public void ConfidenceClassifications_PublishStableCodesAndPreserveVersionedStoredValues() {
        Assert.Multiple(
            () => Assert.Equal("Learning", PredictionConfidence.Learning.Code),
            () => Assert.Equal("Low", PredictionConfidence.Low.Code),
            () => Assert.Equal("Medium", PredictionConfidence.Medium.Code),
            () => Assert.Equal("High", PredictionConfidence.High.Code),
            () => Assert.Equal("Limited", PredictionPatternConsistency.Limited.Code),
            () => Assert.Equal(" future confidence ", PredictionConfidence.FromStoredCode(" future confidence ").Code),
            () => Assert.Equal("future_reason", PredictionReasonCode.FromStoredCode("future_reason").Code));
    }

    [Fact]
    public void Assessment_RejectsNullReasonsAndOwnsAnImmutableCopyOfTheInputCollection() {
        var window = PredictionWindow.FromEndpoints(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3));
        Assert.Throws<ArgumentException>("reasons", () => new CyclePredictionAssessment(window, PredictionConfidence.Low,
            PredictionDataSufficiency.Limited, PredictionPatternConsistency.Limited, [null!]));
        PredictionReasonCode reason = PredictionReasonCode.InsufficientCompletedCycles;
        List<PredictionReasonCode> reasons = [reason];

        var assessment = new CyclePredictionAssessment(window, PredictionConfidence.Low,
            PredictionDataSufficiency.Limited, PredictionPatternConsistency.Limited, reasons);
        reasons.Clear();

        Assert.Same(reason, Assert.Single(assessment.Reasons));
        Assert.Throws<NotSupportedException>(() => ((ICollection<PredictionReasonCode>)assessment.Reasons).Clear());
        Assert.Equal(window, assessment.Window);
    }

    [Theory]
    [InlineData(null, "2026-10-08")]
    [InlineData("2026-10-08", null)]
    [InlineData(null, null)]
    public void Window_PreservesPartialAndAbsentEndpoints(string? from, string? to) {
        DateOnly? start = from is null ? null : DateOnly.ParseExact(from, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        DateOnly? end = to is null ? null : DateOnly.ParseExact(to, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var window = PredictionWindow.FromEndpoints(start, end);
        Assert.Equal(start, window.From);
        Assert.Equal(end, window.To);
    }

    [Fact]
    public void Window_RejectsNewReversedRangeButPreservesStoredRange() {
        var start = new DateOnly(2026, 10, 9);
        var end = new DateOnly(2026, 10, 8);
        Assert.Throws<ArgumentException>(() => PredictionWindow.FromEndpoints(start, end));
        Assert.Equal(start, PredictionWindow.FromStoredEndpoints(start, end).From);
    }

    [Fact]
    public void Classifications_PreserveUnknownVersionedCodes() {
        Assert.Equal("FutureConfidence", PredictionConfidence.FromCode("FutureConfidence").Code);
        Assert.Equal("v3_reason", PredictionReasonCode.FromCode("v3_reason").Code);
        Assert.Equal("future raw ", PredictionDataSufficiency.FromStoredCode("future raw ").Code);
        Assert.Equal("", PredictionPatternConsistency.FromStoredCode("").Code);
    }
}
