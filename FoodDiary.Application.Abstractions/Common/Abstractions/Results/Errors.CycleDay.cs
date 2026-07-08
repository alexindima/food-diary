using FoodDiary.Results;

using FoodDiary.Application.Abstractions.Cycles.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class CycleDay {
        public static Error NotFound(DateTime date) => CycleDayErrors.NotFound(date);
    }
}
