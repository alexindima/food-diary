using FoodDiary.Application.Abstractions.Hydration.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class HydrationEntry {
        public static Error NotFound(Guid id) => HydrationEntryErrors.NotFound(id);
    }
}
