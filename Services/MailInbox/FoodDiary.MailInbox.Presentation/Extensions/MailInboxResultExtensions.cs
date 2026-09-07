using FoodDiary.Results;
using FoodDiary.MailInbox.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Extensions;

public static class MailInboxResultExtensions {
    public static IActionResult ToOkActionResult<TValue, THttpResponse>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, THttpResponse> map) =>
        result.IsSuccess
            ? controller.Ok(map(result.Value))
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ToOkActionResult(
        this Result result,
        ControllerBase controller,
        object response) =>
        result.IsSuccess
            ? controller.Ok(response)
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ToNoContentActionResult(
        this Result result,
        ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ErrorResult(Error error, string? traceId) =>
        new ObjectResult(new MailInboxApiErrorHttpResponse(
            error.Code,
            PublicMessage(error),
            traceId,
            IsSafeToExpose(error.Kind) ? error.Details : null)) {
            StatusCode = MapStatusCode(error.Kind ?? ErrorKind.Internal),
        };

    private static string PublicMessage(Error error) =>
        error.Kind switch {
            ErrorKind.ExternalFailure => "A dependent service failed.",
            ErrorKind.Validation or ErrorKind.Unauthorized or ErrorKind.NotFound or ErrorKind.Conflict => error.Message,
            _ => "An unexpected error occurred.",
        };

    private static bool IsSafeToExpose(ErrorKind? kind) =>
        kind is ErrorKind.Validation or ErrorKind.Unauthorized or ErrorKind.NotFound or ErrorKind.Conflict;

    private static int MapStatusCode(ErrorKind kind) =>
        kind switch {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.ExternalFailure => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError,
        };
}
