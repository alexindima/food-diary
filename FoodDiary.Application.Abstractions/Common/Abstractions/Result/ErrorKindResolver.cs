namespace FoodDiary.Application.Abstractions.Common.Abstractions.Result;

public static class ErrorKindResolver {
    private static readonly IReadOnlyDictionary<string, ErrorKind> ExactMappings = new Dictionary<string, ErrorKind>(StringComparer.Ordinal) {
        ["Authentication.TelegramInvalidData"] = ErrorKind.Validation,
        ["Authentication.TelegramNotLinked"] = ErrorKind.NotFound,
        ["Authentication.TelegramAlreadyLinked"] = ErrorKind.Conflict,
        ["Authentication.AdminSsoForbidden"] = ErrorKind.Forbidden,
        ["Ai.Forbidden"] = ErrorKind.Forbidden,
        ["Ai.QuotaExceeded"] = ErrorKind.RateLimited,
        ["Ai.OpenAiFailed"] = ErrorKind.ExternalFailure,
        ["Ai.InvalidResponse"] = ErrorKind.ExternalFailure,
        ["Image.Forbidden"] = ErrorKind.Forbidden,
        ["Image.InUse"] = ErrorKind.Conflict,
        ["Image.StorageError"] = ErrorKind.ExternalFailure,
        ["Validation.Conflict"] = ErrorKind.Conflict,
    };

    public static ErrorKind? Resolve(string? errorCode) {
        if (string.IsNullOrWhiteSpace(errorCode)) {
            return null;
        }

        if (ExactMappings.TryGetValue(errorCode, out var kind)) {
            return kind;
        }

        if (errorCode.StartsWith("Authentication.", StringComparison.Ordinal)) {
            return ErrorKind.Unauthorized;
        }

        if (errorCode.StartsWith("Validation.", StringComparison.Ordinal)) {
            return ErrorKind.Validation;
        }

        if (errorCode.EndsWith(".NotAccessible", StringComparison.Ordinal) ||
            errorCode.EndsWith(".NotFound", StringComparison.Ordinal)) {
            return ErrorKind.NotFound;
        }

        if (errorCode.EndsWith(".AlreadyExists", StringComparison.Ordinal)) {
            return ErrorKind.Conflict;
        }

        return null;
    }
}
