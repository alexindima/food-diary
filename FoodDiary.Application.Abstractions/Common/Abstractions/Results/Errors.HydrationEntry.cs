using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class HydrationEntry {
        public static Error NotFound(Guid id) => HydrationEntryErrors.NotFound(id);

        public static Error NotAccessible(Guid id) => HydrationEntryErrors.NotAccessible(id);

        public static Error AlreadyExists(DateTime timestampUtc) => HydrationEntryErrors.AlreadyExists(timestampUtc);
    }
}
