using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Application.Tests.Common;

[ExcludeFromCodeCoverage]
public sealed class TemporalRangePolicyTests {
    [Fact]
    public void IsPeriodWithinLimit_UsesInclusiveDayLimit() {
        DateTime from = new(2025, 1, 1);

        Assert.Multiple(
            () => Assert.True(TemporalRangePolicy.IsPeriodWithinLimit(from, from.AddDays(365))),
            () => Assert.False(TemporalRangePolicy.IsPeriodWithinLimit(from, from.AddDays(366))),
            () => Assert.False(TemporalRangePolicy.IsPeriodWithinLimit(from, from.AddDays(-1))));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(366, true)]
    [InlineData(367, false)]
    [InlineData(int.MaxValue, false)]
    public void IsQuantizationValid_EnforcesSupportedBounds(int quantizationDays, bool expected) {
        Assert.Equal(expected, TemporalRangePolicy.IsQuantizationValid(quantizationDays));
    }

    [Fact]
    public void GetInclusiveDayEnd_ForMaximumDate_SaturatesAtMaximumValue() {
        DateTime result = TemporalRangePolicy.GetInclusiveDayEnd(DateTime.MaxValue);

        Assert.Equal(DateTime.MaxValue, result);
    }

    [Fact]
    public void TrySubtract_WhenOffsetUnderflows_ReturnsFalse() {
        bool success = TemporalRangePolicy.TrySubtract(DateTime.MinValue, TimeSpan.FromMinutes(1), out DateTime result);

        Assert.Multiple(
            () => Assert.False(success),
            () => Assert.Equal(default, result));
    }

    [Fact]
    public void TryAddDays_WhenValueOverflows_ReturnsFalse() {
        bool success = TemporalRangePolicy.TryAddDays(DateTime.MaxValue, 1, out DateTime result);

        Assert.Multiple(
            () => Assert.False(success),
            () => Assert.Equal(default, result));
    }

    [Fact]
    public void BuildDateBuckets_AtMaximumDate_ReturnsSingleBucketWithoutOverflow() {
        (DateTime start, DateTime end) = Assert.Single(
            TemporalRangePolicy.BuildDateBuckets(DateTime.MaxValue, DateTime.MaxValue, 366));

        Assert.Multiple(
            () => Assert.Equal(DateTime.MaxValue.Date, start),
            () => Assert.Equal(DateTime.MaxValue.Date, end));
    }

    [Fact]
    public void BuildInstantBuckets_AtMaximumInstant_ReturnsSingleBucketWithoutOverflow() {
        (DateTime start, DateTime end) = Assert.Single(
            TemporalRangePolicy.BuildInstantBuckets(DateTime.MaxValue, DateTime.MaxValue, 1));

        Assert.Multiple(
            () => Assert.Equal(DateTime.MaxValue, start),
            () => Assert.Equal(DateTime.MaxValue, end));
    }

    [Fact]
    public void BuildInstantBuckets_WithMultipleBuckets_UsesContiguousInclusiveBoundaries() {
        var from = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddDays(2);

        IReadOnlyList<(DateTime Start, DateTime End)> buckets = TemporalRangePolicy.BuildInstantBuckets(from, to, 1);

        Assert.Collection(
            buckets,
            bucket => Assert.Equal((from, from.AddDays(1).AddTicks(-1)), bucket),
            bucket => Assert.Equal((from.AddDays(1), from.AddDays(2).AddTicks(-1)), bucket),
            bucket => Assert.Equal((to, to), bucket));
    }

    [Fact]
    public void BuildDateBuckets_ForPartialFinalBucket_StopsAtRequestedEnd() {
        DateTime from = new(2025, 1, 1);
        DateTime to = new(2025, 1, 6);

        (DateTime Start, DateTime End)[] buckets = [.. TemporalRangePolicy.BuildDateBuckets(from, to, 4)];

        Assert.Collection(
            buckets,
            bucket => Assert.Equal((from, new DateTime(2025, 1, 4)), bucket),
            bucket => Assert.Equal((new DateTime(2025, 1, 5), to), bucket));
    }

    [Fact]
    public void BuildDateBuckets_WhenPeriodExceedsLimit_Throws() {
        DateTime from = new(2025, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TemporalRangePolicy.BuildDateBuckets(from, from.AddDays(366), 1).ToArray());
    }

    [Fact]
    public void GetInclusiveDayCount_WhenRangeIsReversed_Throws() {
        DateTime date = new(2025, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TemporalRangePolicy.GetInclusiveDayCount(date, date.AddDays(-1)));
    }

    [Theory]
    [InlineData(0, -1, 1)]
    [InlineData(0, 1, 0)]
    [InlineData(0, 1, 367)]
    public void BuildDateBuckets_WithInvalidArguments_Throws(
        int fromOffsetDays,
        int toOffsetDays,
        int quantizationDays) {
        DateTime date = new(2025, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TemporalRangePolicy.BuildDateBuckets(
                date.AddDays(fromOffsetDays),
                date.AddDays(toOffsetDays),
                quantizationDays).ToArray());
    }
}
