namespace FoodDiary.Application.Abstractions.Common.Abstractions.Results;

public static partial class Errors {
    public static class Validation {
        public static Error Required(string field) => new(
            "Validation.Required",
            $"Field {field} is required.",
            CreateDetails(field, $"Field {field} is required."),
            Kind: ErrorKind.Validation);

        public static Error Invalid(string field, string reason) => new(
            "Validation.Invalid",
            $"Field {field} is invalid: {reason}",
            CreateDetails(field, $"Field {field} is invalid: {reason}"),
            Kind: ErrorKind.Validation);

        private static IReadOnlyDictionary<string, string[]> CreateDetails(string field, string message) =>
            new Dictionary<string, string[]>(StringComparer.Ordinal) {
                [field] = [message],
            };
    }
}
