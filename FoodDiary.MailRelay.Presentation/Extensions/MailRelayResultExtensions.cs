using FoodDiary.MailRelay.Application.Common.Result;
using FoodDiary.MailRelay.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Extensions;

public static class MailRelayResultExtensions {
    public static IActionResult ToOkActionResult<TValue, THttpResponse>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, THttpResponse> map) =>
        result.IsSuccess
            ? controller.Ok(map(result.Value))
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ToCreatedActionResult<TValue, THttpResponse>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, string> locationFactory,
        Func<TValue, THttpResponse> map) =>
        result.IsSuccess
            ? controller.Created(locationFactory(result.Value), map(result.Value))
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ToAcceptedActionResult<TValue, THttpResponse>(
        this Result<TValue> result,
        ControllerBase controller,
        Func<TValue, string> locationFactory,
        Func<TValue, THttpResponse> map) =>
        result.IsSuccess
            ? controller.Accepted(locationFactory(result.Value), map(result.Value))
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ToNoContentActionResult(
        this Result result,
        ControllerBase controller) =>
        result.IsSuccess
            ? controller.NoContent()
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ToOkActionResult(
        this Result result,
        ControllerBase controller,
        object response) =>
        result.IsSuccess
            ? controller.Ok(response)
            : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);

    public static IActionResult ErrorResult(MailRelayError error, string? traceId) =>
        new ObjectResult(new MailRelayApiErrorHttpResponse(
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
