using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Validation {
        public static Error Required(string field) => new(
            "Validation.Required",
            $"Field {field} is required.",
            Kind: ErrorKind.Validation,
            Details: CreateDetails(field, $"Field {field} is required."));

        public static Error Invalid(string field, string reason) => new(
            "Validation.Invalid",
            $"Field {field} is invalid: {reason}",
            Kind: ErrorKind.Validation,
            Details: CreateDetails(field, $"Field {field} is invalid: {reason}"));

        private static IReadOnlyDictionary<string, string[]> CreateDetails(string field, string message) =>
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                [field] = [message],
            };
    }
}
