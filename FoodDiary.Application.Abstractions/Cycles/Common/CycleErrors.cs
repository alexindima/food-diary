using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Abstractions.Cycles.Common;

public static class CycleErrors {
    public static Error NotFound(Guid id) => new(
        "Cycle.NotFound",
        $"Cycle with ID {id} was not found.",
        Kind: ErrorKind.NotFound);
}
