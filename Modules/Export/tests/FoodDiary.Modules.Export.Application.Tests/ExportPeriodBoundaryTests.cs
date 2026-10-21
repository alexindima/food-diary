using FoodDiary.Modules.Export.Application.Queries.ExportCycle;
using FoodDiary.Modules.Export.Application.Queries.ExportDiary;

namespace FoodDiary.Modules.Export.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExportPeriodBoundaryTests {
    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void CalendarPeriod_PreservesInclusiveLeapYearEndpoints(int days) {
        var from = new DateOnly(2024, 1, 1);
        DateOnly to = from.AddDays(days);

        var period = CycleExportPeriod.FromDates(from, to);

        Assert.Equal(from, period.From);
        Assert.Equal(to, period.To);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(367)]
    public void CalendarPeriod_RejectsReversedOrExcessiveRanges(int days) {
        var from = new DateOnly(2024, 1, 1);

        Assert.Throws<ArgumentException>("from", () => CycleExportPeriod.FromDates(from, from.AddDays(days)));
    }

    [Theory]
    [InlineData(-840)]
    [InlineData(0)]
    [InlineData(840)]
    public void DisplayOffset_PreservesTheInclusiveFourteenHourBounds(int minutes) {
        var offset = DiaryDisplayOffset.FromResolvedValue(TimeSpan.FromMinutes(minutes));

        Assert.Equal(TimeSpan.FromMinutes(minutes), offset.Value);
    }

    [Theory]
    [InlineData(-841)]
    [InlineData(841)]
    public void DisplayOffset_RejectsValuesOutsideTheSupportedRange(int minutes) =>
        Assert.Throws<ArgumentOutOfRangeException>("value", () => DiaryDisplayOffset.FromResolvedValue(TimeSpan.FromMinutes(minutes)));

    [Fact]
    public void DiaryPeriod_PreservesNormalizedInstantsAndTheSeparateDisplayOffsetAtTheExactRangeLimit() {
        var from = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var offset = DiaryDisplayOffset.FromResolvedValue(TimeSpan.FromHours(14));

        var period = DiaryExportPeriod.FromResolvedRange(from, to, offset);

        Assert.Multiple(
            () => Assert.Equal(from, period.From),
            () => Assert.Equal(to, period.To),
            () => Assert.Same(offset, period.DisplayOffset));
        Assert.Throws<ArgumentNullException>("displayOffset", () => DiaryExportPeriod.FromResolvedRange(from, to, null!));
        Assert.Throws<ArgumentException>("from", () => DiaryExportPeriod.FromResolvedRange(to, from, offset));
        Assert.Throws<ArgumentException>("from", () => DiaryExportPeriod.FromResolvedRange(from, to.AddTicks(1), offset));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DiaryPeriod_RejectsDisplayDatesThatWouldOverflowEvenForAZeroLengthRange(bool upperBound) {
        var instant = DateTime.SpecifyKind(upperBound ? DateTime.MaxValue : DateTime.MinValue, DateTimeKind.Utc);
        var offset = DiaryDisplayOffset.FromResolvedValue(TimeSpan.FromHours(upperBound ? 14 : -14));

        Assert.Throws<ArgumentOutOfRangeException>(() => DiaryExportPeriod.FromResolvedRange(instant, instant, offset));
    }
}
