using System.Diagnostics;
using FoodDiary.Application.Common.Abstractions.Result;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Presentation.Api.Responses;

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

    public static IActionResult ToErrorActionResult(this Error error, int statusCode) =>
        ErrorResult(error, statusCode);

    private static IActionResult ErrorResult(Error error) =>
        ErrorResult(error, PresentationErrorHttpMapper.MapStatusCode(error));

    private static IActionResult ErrorResult(Error error, int statusCode) =>
        new ObjectResult(new ApiErrorHttpResponse(error.Code, error.Message, Activity.Current?.Id)) {
            StatusCode = statusCode,
        };
}
