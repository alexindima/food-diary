using FoodDiary.Modules.BodyMetrics.Domain.ValueObjects;

namespace FoodDiary.Modules.BodyMetrics.Application.Common;

internal static class UtcDateNormalizer {
    public static DateTime NormalizeDatePreservingUnspecifiedAsUtc(DateTime value) =>
        MeasurementDay.FromDateTimeEncoding(value).ToUtcDateTime();
}
