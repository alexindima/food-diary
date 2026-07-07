using FoodDiary.Application.Abstractions.Ai.Common;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Ai {
        public static Error ImageNotFound(Guid id) => AiErrors.ImageNotFound(id);

        public static Error Forbidden() => AiErrors.Forbidden();

        public static Error EmptyItems() => AiErrors.EmptyItems();

        public static Error OpenAiFailed(string reason) => AiErrors.OpenAiFailed(reason);

        public static Error InvalidResponse(string reason) => AiErrors.InvalidResponse(reason);

        public static Error QuotaExceeded() => AiErrors.QuotaExceeded();
    }
}
