using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Cycles.Common;

public static class CycleDayErrors {
    public static Error NotFound(DateTime date) => new(
        "CycleDay.NotFound",
        $"Cycle day for {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} was not found.",
        Kind: ErrorKind.NotFound);
}
