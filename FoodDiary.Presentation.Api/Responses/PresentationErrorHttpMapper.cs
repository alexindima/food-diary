using FoodDiary.Application.Common.Abstractions.Result;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Responses;

public static class PresentationErrorHttpMapper {
    public static int MapStatusCode(Error error) {
        if (error.Kind is { } kind) {
            return MapStatusCode(kind);
        }

        if (ErrorKindResolver.Resolve(error.Code) is { } resolvedKind) {
            return MapStatusCode(resolvedKind);
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
}
