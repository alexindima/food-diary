namespace FoodDiary.Modules.Statistics.Application.Common;

internal sealed record StatisticsBodyPeriod {
    public DateTime From { get; }
    public DateTime To { get; }

    private StatisticsBodyPeriod(DateTime from, DateTime to) {
        From = from;
        To = to;
    }

    public static StatisticsBodyPeriod Resolve(DateOnly? from, DateOnly? to, DateTime fallbackFrom, DateTime fallbackTo) {
        if (!BodyMetricDateRangePolicy.IsValid(from, to)) {
            throw new ArgumentException("Invalid body metric calendar period.", nameof(from));
        }
        return new StatisticsBodyPeriod(
            from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(fallbackFrom),
            to?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? UtcDateNormalizer.NormalizeDatePreservingUnspecifiedAsUtc(fallbackTo));
    }
}
