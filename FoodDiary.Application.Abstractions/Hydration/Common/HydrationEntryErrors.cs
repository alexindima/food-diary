using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Hydration.Common;

public static class HydrationEntryErrors {
    public static Error NotFound(Guid id) => new(
        "HydrationEntry.NotFound",
        $"Hydration entry with id '{id}' not found",
        Kind: ErrorKind.NotFound);

    public static Error NotAccessible(Guid id) => new(
        "HydrationEntry.NotAccessible",
        $"Hydration entry with id '{id}' was not found or is not accessible.",
        Kind: ErrorKind.NotFound);
}
