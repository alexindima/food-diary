using FoodDiary.Results;

using FoodDiary.Application.Abstractions.WaistEntries.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class WaistEntry {
        public static Error NotFound(Guid id) => WaistEntryErrors.NotFound(id);

        public static Error AlreadyExists(DateTime date) => WaistEntryErrors.AlreadyExists(date);
    }
}
