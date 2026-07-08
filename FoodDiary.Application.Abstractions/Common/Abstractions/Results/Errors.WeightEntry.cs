using FoodDiary.Results;

using FoodDiary.Application.Abstractions.WeightEntries.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class WeightEntry {
        public static Error NotFound(Guid id) => WeightEntryErrors.NotFound(id);

        public static Error AlreadyExists(DateTime date) => WeightEntryErrors.AlreadyExists(date);
    }
}
