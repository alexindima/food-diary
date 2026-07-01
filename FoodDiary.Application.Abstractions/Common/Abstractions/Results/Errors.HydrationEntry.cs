namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class HydrationEntry {
        public static Error NotFound(Guid id) => new(
            "HydrationEntry.NotFound",
            $"Hydration entry with id '{id}' not found",
            Kind: ErrorKind.NotFound);
    }
}
