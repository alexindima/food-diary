using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Idempotency {
        public static Error Conflict => new(
            "Idempotency.Conflict",
            "The idempotency key was already used with a different request.",
            Kind: ErrorKind.Conflict);
    }
}
