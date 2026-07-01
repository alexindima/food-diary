namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Ai {
        public static Error ImageNotFound(Guid id) => new(
            "Ai.ImageNotFound",
            $"Image asset with ID {id} was not found.",
            Kind: ErrorKind.NotFound);

        public static Error Forbidden() => new(
            "Ai.Forbidden",
            "Image asset does not belong to the current user.",
            Kind: ErrorKind.Forbidden);

        public static Error EmptyItems() => new(
            "Ai.EmptyItems",
            "No food items were provided.",
            Kind: ErrorKind.Validation);

        public static Error OpenAiFailed(string reason) => new(
            "Ai.OpenAiFailed",
            reason,
            Kind: ErrorKind.ExternalFailure);

        public static Error InvalidResponse(string reason) => new(
            "Ai.InvalidResponse",
            reason,
            Kind: ErrorKind.ExternalFailure);

        public static Error QuotaExceeded() => new(
            "Ai.QuotaExceeded",
            "AI token quota exceeded for the current month.",
            Kind: ErrorKind.RateLimited);
    }
}
