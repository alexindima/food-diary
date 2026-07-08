using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public static class HydrationEntryErrors {
    public static Error NotFound(Guid id) => new(
        "HydrationEntry.NotFound",
        $"Hydration entry with id '{id}' not found",
        Kind: ErrorKind.NotFound);
}
