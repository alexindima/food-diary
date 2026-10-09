namespace FoodDiary.Modules.Statistics.Application.Common;

internal sealed record StatisticsInstantPeriod {
    public DateTime From { get; }
    public DateTime To { get; }
    public string? TimeZoneId { get; }

    private StatisticsInstantPeriod(DateTime from, DateTime to, string? timeZoneId) {
        From = from;
        To = to;
        TimeZoneId = timeZoneId;
    }

    public static StatisticsInstantPeriod FromRequest(DateTime from, DateTime to, string? timeZoneId) {
        if (from > to || !StatisticsCalendarRangePolicy.IsTimeZoneValid(timeZoneId) ||
            !StatisticsCalendarRangePolicy.IsPeriodWithinLimit(from, to, timeZoneId)) {
            throw new ArgumentException("Invalid statistics period.", nameof(from));
        }
        return new StatisticsInstantPeriod(UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(from),
            UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(to), timeZoneId);
    }
}
