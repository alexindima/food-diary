using FoodDiary.Application.Abstractions.Cycles.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Cycle {
        public static Error NotFound(Guid id) => CycleErrors.NotFound(id);
    }
}
