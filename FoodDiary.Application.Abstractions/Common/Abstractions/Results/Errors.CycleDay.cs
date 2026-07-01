using System.Globalization;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class CycleDay {
        public static Error NotFound(DateTime date) => new(
            "CycleDay.NotFound",
            $"Cycle day for {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} was not found.",
            Kind: ErrorKind.NotFound);
    }
}
