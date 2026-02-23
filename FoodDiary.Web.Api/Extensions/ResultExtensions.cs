using FoodDiary.Application.Common.Abstractions.Result;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.Extensions;

public static class ResultExtensions {
    public static IActionResult ToActionResult<T>(this Result<T> result) {
        if (result.IsSuccess) {
            return new OkObjectResult(result.Value);
        }

        var code = result.Error.Code;

        return code switch {
            "Authentication.TelegramInvalidData" => ErrorResult(result.Error, StatusCodes.Status400BadRequest),
            "Authentication.TelegramAuthExpired" => ErrorResult(result.Error, StatusCodes.Status401Unauthorized),
            "Authentication.TelegramNotLinked" => ErrorResult(result.Error, StatusCodes.Status404NotFound),
            "Authentication.TelegramAlreadyLinked" => ErrorResult(result.Error, StatusCodes.Status409Conflict),
            "Authentication.AdminSsoForbidden" => ErrorResult(result.Error, StatusCodes.Status403Forbidden),
            "Authentication.AdminSsoInvalidCode" => ErrorResult(result.Error, StatusCodes.Status401Unauthorized),
            "Ai.Forbidden" => ErrorResult(result.Error, StatusCodes.Status403Forbidden),
            "Ai.QuotaExceeded" => ErrorResult(result.Error, StatusCodes.Status429TooManyRequests),
            "Ai.OpenAiFailed" or "Ai.InvalidResponse" => ErrorResult(result.Error, StatusCodes.Status502BadGateway),
            _ when code.StartsWith("Authentication.", StringComparison.Ordinal) =>
                ErrorResult(result.Error, StatusCodes.Status401Unauthorized),
            _ when code.StartsWith("Validation.", StringComparison.Ordinal) =>
                ErrorResult(result.Error, StatusCodes.Status400BadRequest),
            _ when code.EndsWith(".AlreadyExists", StringComparison.Ordinal) =>
                ErrorResult(result.Error, StatusCodes.Status409Conflict),
            _ when code.EndsWith(".NotFound", StringComparison.Ordinal) =>
                ErrorResult(result.Error, StatusCodes.Status404NotFound),
            _ => ErrorResult(result.Error, StatusCodes.Status500InternalServerError),
        };
    }

    private static IActionResult ErrorResult(Error error, int statusCode) =>
        new ObjectResult(new {
            error = error.Code,
            message = error.Message,
        }) {
            StatusCode = statusCode,
        };
}
