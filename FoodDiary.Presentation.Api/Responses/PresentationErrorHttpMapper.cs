using FoodDiary.Application.Common.Abstractions.Result;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Responses;

public static class PresentationErrorHttpMapper {
    private static readonly IReadOnlyDictionary<string, int> ExactMappings = new Dictionary<string, int>(StringComparer.Ordinal) {
        ["Authentication.TelegramInvalidData"] = StatusCodes.Status400BadRequest,
        ["Authentication.TelegramAuthExpired"] = StatusCodes.Status401Unauthorized,
        ["Authentication.TelegramNotLinked"] = StatusCodes.Status404NotFound,
        ["Authentication.TelegramAlreadyLinked"] = StatusCodes.Status409Conflict,
        ["Authentication.TelegramNotConfigured"] = StatusCodes.Status500InternalServerError,
        ["Authentication.TelegramBotNotConfigured"] = StatusCodes.Status500InternalServerError,
        ["Authentication.TelegramBotInvalidSecret"] = StatusCodes.Status401Unauthorized,
        ["Authentication.AdminSsoForbidden"] = StatusCodes.Status403Forbidden,
        ["Authentication.AdminSsoInvalidCode"] = StatusCodes.Status401Unauthorized,
        ["Authentication.AccountNotDeleted"] = StatusCodes.Status409Conflict,
        ["Ai.Forbidden"] = StatusCodes.Status403Forbidden,
        ["Ai.ImageNotFound"] = StatusCodes.Status404NotFound,
        ["Ai.EmptyItems"] = StatusCodes.Status400BadRequest,
        ["Ai.QuotaExceeded"] = StatusCodes.Status429TooManyRequests,
        ["Ai.OpenAiFailed"] = StatusCodes.Status502BadGateway,
        ["Ai.InvalidResponse"] = StatusCodes.Status502BadGateway,
        ["Image.InvalidData"] = StatusCodes.Status400BadRequest,
        ["Image.Forbidden"] = StatusCodes.Status403Forbidden,
        ["Image.InUse"] = StatusCodes.Status409Conflict,
        ["Image.StorageError"] = StatusCodes.Status502BadGateway,
        ["User.InvalidPassword"] = StatusCodes.Status401Unauthorized,
        ["User.InvalidCredentials"] = StatusCodes.Status401Unauthorized,
        ["User.EmailAlreadyExists"] = StatusCodes.Status409Conflict,
        ["Validation.Conflict"] = StatusCodes.Status409Conflict,
    };

    private static readonly Rule[] ConventionRules = [
        new(static code => code.StartsWith("Authentication.", StringComparison.Ordinal), StatusCodes.Status401Unauthorized),
        new(static code => code.StartsWith("Validation.", StringComparison.Ordinal), StatusCodes.Status400BadRequest),
        new(static code => code.EndsWith(".NotAccessible", StringComparison.Ordinal), StatusCodes.Status404NotFound),
        new(static code => code.EndsWith(".AlreadyExists", StringComparison.Ordinal), StatusCodes.Status409Conflict),
        new(static code => code.EndsWith(".NotFound", StringComparison.Ordinal), StatusCodes.Status404NotFound),
    ];

    public static int MapStatusCode(Error error) {
        if (ExactMappings.TryGetValue(error.Code, out var statusCode)) {
            return statusCode;
        }

        foreach (var rule in ConventionRules) {
            if (rule.Matches(error.Code)) {
                return rule.StatusCode;
            }
        }

        return StatusCodes.Status500InternalServerError;
    }

    private sealed record Rule(Func<string, bool> Matches, int StatusCode);
}
