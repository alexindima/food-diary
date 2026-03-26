using FoodDiary.Application.Common.Abstractions.Result;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Responses;

public static class PresentationErrorHttpMapper {
    private static readonly Rule[] ConventionRules = [
        new(static code => code.StartsWith("Authentication.", StringComparison.Ordinal), StatusCodes.Status401Unauthorized),
        new(static code => code.StartsWith("Validation.", StringComparison.Ordinal), StatusCodes.Status400BadRequest),
        new(static code => code.EndsWith(".NotAccessible", StringComparison.Ordinal), StatusCodes.Status404NotFound),
        new(static code => code.EndsWith(".AlreadyExists", StringComparison.Ordinal), StatusCodes.Status409Conflict),
        new(static code => code.EndsWith(".NotFound", StringComparison.Ordinal), StatusCodes.Status404NotFound),
    ];

    public static int MapStatusCode(Error error) {
        if (error.Kind is { } kind) {
            return MapStatusCode(kind);
        }

        foreach (var rule in ConventionRules) {
            if (rule.Matches(error.Code)) {
                return rule.StatusCode;
            }
        }

        return StatusCodes.Status500InternalServerError;
    }

    private static int MapStatusCode(ErrorKind kind) =>
        kind switch {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.RateLimited => StatusCodes.Status429TooManyRequests,
            ErrorKind.ExternalFailure => StatusCodes.Status502BadGateway,
            ErrorKind.Internal => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };

    private sealed record Rule(Func<string, bool> Matches, int StatusCode);
}
