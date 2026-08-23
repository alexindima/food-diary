namespace FoodDiary.Application.Abstractions.Common.Validation;

public static class TemporalRangePolicy {
    public const int MaxPeriodDays = 366;
    public const int MaxQuantizationDays = 366;

    public static bool IsPeriodWithinLimit(DateTime dateFrom, DateTime dateTo) =>
        dateFrom <= dateTo && GetInclusiveDayCount(dateFrom, dateTo) <= MaxPeriodDays;

    public static bool IsQuantizationValid(int quantizationDays) =>
        quantizationDays is >= 1 and <= MaxQuantizationDays;

    public static int GetInclusiveDayCount(DateTime dateFrom, DateTime dateTo) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dateFrom, dateTo);
        return (dateTo.Date - dateFrom.Date).Days + 1;
    }

    public static DateTime GetInclusiveDayEnd(DateTime value) {
        DateTime date = value.Date;
        if (date == DateTime.MaxValue.Date) {
            return DateTime.SpecifyKind(DateTime.MaxValue, value.Kind);
        }

        return date.AddDays(1).AddTicks(-1);
    }

    public static bool TrySubtract(DateTime value, TimeSpan offset, out DateTime result) {
        try {
            result = value.Subtract(offset);
            return true;
        } catch (ArgumentOutOfRangeException) {
            result = default;
            return false;
        }
    }

    public static bool TryAddDays(DateTime value, int days, out DateTime result) {
        try {
            result = value.AddDays(days);
            return true;
        } catch (ArgumentOutOfRangeException) {
            result = default;
            return false;
        }
    }

    public static IEnumerable<(DateTime Start, DateTime End)> BuildDateBuckets(
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays) {
        ValidateBucketArguments(dateFrom, dateTo, quantizationDays);

        DateTime current = dateFrom.Date;
        DateTime end = dateTo.Date;

        // Invariant: current <= end holds on entry (validated above) and is re-established
        // at the bottom of every iteration that doesn't exit via yield break, so the loop
        // never falls out through a false condition check; while (true) makes that explicit.
        while (true) {
            int remainingDays = (end - current).Days;
            int bucketDays = Math.Min(quantizationDays, remainingDays + 1);
            DateTime bucketEnd = current.AddDays(bucketDays - 1);

            yield return (current, bucketEnd);
            if (bucketEnd == end) {
                yield break;
            }

            current = bucketEnd.AddDays(1);
        }
    }

    public static IReadOnlyList<(DateTime Start, DateTime End)> BuildInstantBuckets(
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays) {
        ValidateBucketArguments(dateFrom, dateTo, quantizationDays);

        var buckets = new List<(DateTime Start, DateTime End)>();
        long bucketLengthTicks = TimeSpan.FromDays(quantizationDays).Ticks;
        DateTime current = dateFrom;
        while (current <= dateTo) {
            long remainingTicks = dateTo.Ticks - current.Ticks;
            long bucketTicks = Math.Min(bucketLengthTicks, remainingTicks + 1);
            DateTime bucketEnd = current.AddTicks(bucketTicks - 1);

            buckets.Add((current, bucketEnd));
            if (bucketEnd == dateTo) {
                break;
            }

            current = bucketEnd.AddTicks(1);
        }

        return buckets;
    }

    private static void ValidateBucketArguments(DateTime dateFrom, DateTime dateTo, int quantizationDays) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dateFrom, dateTo);
        ArgumentOutOfRangeException.ThrowIfLessThan(quantizationDays, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantizationDays, MaxQuantizationDays);

        if (!IsPeriodWithinLimit(dateFrom, dateTo)) {
            throw new ArgumentOutOfRangeException(
                nameof(dateTo),
                $"The period must not exceed {MaxPeriodDays} days.");
        }
    }
}
