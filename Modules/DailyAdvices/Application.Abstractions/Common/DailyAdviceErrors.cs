using FoodDiary.Results;

namespace FoodDiary.Modules.DailyAdvices.Application.Abstractions.Common;

public static class DailyAdviceErrors {
    public static Error NotFound(string? locale = null) => new(
        "DailyAdvice.NotFound",
        locale is null
            ? "Daily advice items are not configured."
            : $"Daily advice items are not configured for locale '{locale}'.",
        Kind: ErrorKind.NotFound);
}
