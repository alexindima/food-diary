using FoodDiary.Application.Common.Abstractions.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ResultExtensions {
    public static IActionResult ToActionResult(this Result result) {
        return result.IsSuccess ? new OkResult() : ErrorResult(result.Error);
    }

    extension<T>(Result<T> result) {
        public IActionResult ToActionResult() {
            return result.IsSuccess ? new OkObjectResult(result.Value) : ErrorResult(result.Error);
        }

        public IActionResult ToOkActionResult(ControllerBase controller) {
            return result.IsSuccess
                ? controller.Ok(result.Value)
                : ErrorResult(result.Error);
        }

        public IActionResult ToOkActionResult<TResponse>(ControllerBase controller,
            Func<T, TResponse> map) {
            return result.IsSuccess
                ? controller.Ok(map(result.Value))
                : ErrorResult(result.Error);
        }
    }

    public static IActionResult ToNoContentActionResult(this Result result) {
        return result.IsSuccess
            ? new NoContentResult()
            : ErrorResult(result.Error);
    }

    private static IActionResult ErrorResult(Error error) {
        var code = error.Code;

        return code switch {
            "Authentication.TelegramInvalidData" => ErrorResult(error, StatusCodes.Status400BadRequest),
            "Authentication.TelegramAuthExpired" => ErrorResult(error, StatusCodes.Status401Unauthorized),
            "Authentication.TelegramNotLinked" => ErrorResult(error, StatusCodes.Status404NotFound),
            "Authentication.TelegramAlreadyLinked" => ErrorResult(error, StatusCodes.Status409Conflict),
            "Authentication.AdminSsoForbidden" => ErrorResult(error, StatusCodes.Status403Forbidden),
            "Authentication.AdminSsoInvalidCode" => ErrorResult(error, StatusCodes.Status401Unauthorized),
            "Ai.Forbidden" => ErrorResult(error, StatusCodes.Status403Forbidden),
            "Ai.QuotaExceeded" => ErrorResult(error, StatusCodes.Status429TooManyRequests),
            "Ai.OpenAiFailed" or "Ai.InvalidResponse" => ErrorResult(error, StatusCodes.Status502BadGateway),
            "Image.Forbidden" => ErrorResult(error, StatusCodes.Status403Forbidden),
            "Image.InUse" => ErrorResult(error, StatusCodes.Status409Conflict),
            "Image.StorageError" => ErrorResult(error, StatusCodes.Status502BadGateway),
            _ when code.StartsWith("Authentication.", StringComparison.Ordinal) =>
                ErrorResult(error, StatusCodes.Status401Unauthorized),
            _ when code.StartsWith("Validation.", StringComparison.Ordinal) =>
                ErrorResult(error, StatusCodes.Status400BadRequest),
            _ when code.EndsWith(".AlreadyExists", StringComparison.Ordinal) =>
                ErrorResult(error, StatusCodes.Status409Conflict),
            _ when code.EndsWith(".NotFound", StringComparison.Ordinal) =>
                ErrorResult(error, StatusCodes.Status404NotFound),
            _ => ErrorResult(error, StatusCodes.Status500InternalServerError),
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
