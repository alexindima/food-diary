using FoodDiary.MailInbox.Application.Common.Result;
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

    public static IActionResult ErrorResult(MailInboxError error, string? traceId) =>
        new ObjectResult(new MailInboxApiErrorHttpResponse(
            error.Code,
            error.Message,
            traceId,
            error.Details)) {
            StatusCode = MapStatusCode(error.Kind),
        };

    private static int MapStatusCode(ErrorKind kind) =>
        kind switch {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.ExternalFailure => StatusCodes.Status502BadGateway,
            ErrorKind.Internal => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };
}
