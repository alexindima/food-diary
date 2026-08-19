using System.Diagnostics;
using FoodDiary.Results;
using FoodDiary.Application.Export.Models;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ResultExtensions {
    extension(Result result) {
        public IActionResult ToActionResult() {
            return result.IsSuccess ? new OkResult() : ErrorResult(result.Error);
        }

        public IActionResult ToNoContentActionResult(ControllerBase controller) {
            return result.IsSuccess
                ? new NoContentResult()
                : ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
        }
    }

    extension<T>(Result<T> result) {
        public IActionResult ToActionResult() {
            return result.IsSuccess ? new OkObjectResult(result.Value) : ErrorResult(result.Error);
        }

        public IActionResult ToOkActionResult(ControllerBase controller) {
            return result.IsSuccess
                ? controller.Ok(result.Value)
                : ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
        }

        public IActionResult ToOkActionResult<TResponse>(ControllerBase controller,
            Func<T, TResponse> map) {
            return result.IsSuccess
                ? controller.Ok(map(result.Value))
                : ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
        }

        public IActionResult ToOptionalActionResult<TResponse>(
            ControllerBase controller,
            Func<T, TResponse?> map)
            where TResponse : class {
            if (result.IsFailure) {
                return ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
            }

            TResponse? response = map(result.Value);
            return response is null ? controller.NoContent() : controller.Ok(response);
        }

        public IActionResult ToCreatedAtActionResult<TResponse>(
            ControllerBase controller,
            string actionName,
            Func<T, object?> routeValues,
            Func<T, TResponse> map) {
            return result.IsSuccess
                ? controller.CreatedAtAction(actionName, routeValues(result.Value), map(result.Value))
                : ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
        }

        public IActionResult ToCreatedResult<TResponse>(
            ControllerBase controller,
            Func<T, TResponse> map) {
            return result.IsSuccess
                ? new CreatedResult(location: (string?)null, value: map(result.Value))
                : ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
        }

        public IActionResult ToAcceptedActionResult<TResponse>(
            ControllerBase controller,
            Func<T, TResponse> map) {
            return result.IsSuccess
                ? controller.Accepted(map(result.Value))
                : ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
        }
    }

    extension(Result<FileExportResult> result) {
        public IActionResult ToFileActionResult(ControllerBase controller) {
            if (result.IsFailure) {
                return ErrorResult(result.Error, controller.HttpContext.TraceIdentifier);
            }

            FileExportResult file = result.Value;
            return controller.File(file.Content, file.ContentType, file.FileName);
        }
    }

    extension(Error error) {
        public IActionResult ToErrorActionResult(int statusCode) =>
                ErrorResult(error, statusCode);
    }

    private static IActionResult ErrorResult(Error error) =>
        ErrorResult(error, PresentationErrorHttpMapper.MapStatusCode(error));

    private static IActionResult ErrorResult(Error error, int statusCode) =>
        ErrorResult(error, statusCode, Activity.Current?.Id);

    private static IActionResult ErrorResult(Error error, string? traceId) =>
        ErrorResult(error, PresentationErrorHttpMapper.MapStatusCode(error), traceId);

    private static IActionResult ErrorResult(Error error, int statusCode, string? traceId) =>
        new ObjectResult(PresentationErrorHttpMapper.MapResponse(error, traceId)) {
            StatusCode = statusCode,
        };
}
