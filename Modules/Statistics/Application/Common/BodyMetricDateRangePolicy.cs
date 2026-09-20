using FoodDiary.Application.Abstractions.Common.Validation;

namespace FoodDiary.Modules.Statistics.Application.Common;

internal static class BodyMetricDateRangePolicy {
    public static bool IsValid(DateOnly? from, DateOnly? to) =>
        (from is null && to is null) ||
        (from is not null && to is not null && from <= to &&
        to.Value.DayNumber - from.Value.DayNumber <= TemporalRangePolicy.MaxPeriodDays);
}
