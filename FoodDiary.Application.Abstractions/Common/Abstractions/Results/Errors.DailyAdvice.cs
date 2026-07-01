namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class DailyAdvice {
        public static Error NotFound(string? locale = null) => new(
            "DailyAdvice.NotFound",
            locale is null
                ? "Daily advice items are not configured."
                : $"Daily advice items are not configured for locale '{locale}'.",
            Kind: ErrorKind.NotFound);
    }
}
