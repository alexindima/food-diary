using FoodDiary.Results;
using FoodDiary.MailRelay.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Extensions;

public static class MailRelayResultExtensions {
    extension<TValue>(Result<TValue> result) {
        public IActionResult ToOkActionResult<THttpResponse>(ControllerBase controller,
            Func<TValue, THttpResponse> map) =>
            result.IsSuccess
                ? controller.Ok(map(result.Value))
                : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);
        public IActionResult ToCreatedActionResult<THttpResponse>(ControllerBase controller,
            Func<TValue, string> locationFactory,
            Func<TValue, THttpResponse> map) =>
            result.IsSuccess
                ? controller.Created(locationFactory(result.Value), map(result.Value))
                : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);
        public IActionResult ToAcceptedActionResult<THttpResponse>(ControllerBase controller,
            Func<TValue, string> locationFactory,
            Func<TValue, THttpResponse> map) =>
            result.IsSuccess
                ? controller.Accepted(locationFactory(result.Value), map(result.Value))
                : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);
    }

    extension(Result result) {
        public IActionResult ToNoContentActionResult(ControllerBase controller) =>
            result.IsSuccess
                ? controller.NoContent()
                : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);
        public IActionResult ToOkActionResult(ControllerBase controller,
            object response) =>
            result.IsSuccess
                ? controller.Ok(response)
                : ErrorResult(result.Error!, controller.HttpContext.TraceIdentifier);
    }

    public static IActionResult ErrorResult(Error error, string? traceId) =>
        new ObjectResult(new MailRelayApiErrorHttpResponse(
            error.Code,
            error.Message,
            traceId,
            error.Details)) {
            StatusCode = MapStatusCode(error.Kind ?? ErrorKind.Internal),
        };

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
