using FoodDiary.Application.Abstractions.DailyAdvices.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class DailyAdvice {
        public static Error NotFound(string? locale = null) => DailyAdviceErrors.NotFound(locale);
    }
}
