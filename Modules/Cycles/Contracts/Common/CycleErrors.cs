using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Contracts.Common;

public static class CycleErrors {
    public static Error NotFound(Guid id) => new(
        "Cycle.NotFound",
        $"Cycle with ID {id} was not found.",
        Kind: ErrorKind.NotFound);
}
