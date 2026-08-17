using Asp.Versioning;
using FoodDiary.Results;
using FoodDiary.Application.Export.Models;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Controllers;

[ApiVersion("1.0")]
public abstract class BaseApiController(ISender mediator) : ControllerBase {
    protected Task Send(IRequest request) {
        return mediator.Send(request, HttpContext.RequestAborted);
    }

    private Task<TResponse> Send<TResponse>(IRequest<TResponse> request) {
        return mediator.Send(request, HttpContext.RequestAborted);
    }

    protected async Task<IActionResult> HandleOk<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        Result<TResponse> result = await Send(request);
        return result.ToOkActionResult(this, map);
    }

    protected async Task<IActionResult> HandleCreated<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        string actionName,
        Func<TResponse, object?> routeValues,
        Func<TResponse, THttpResponse> map) {
        Result<TResponse> result = await Send(request);
        return result.ToCreatedAtActionResult(this, actionName, routeValues, map);
    }

    protected async Task<IActionResult> HandleCreated<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        Result<TResponse> result = await Send(request);
        return result.ToCreatedActionResult(this, map);
    }

    protected async Task<IActionResult> HandleAccepted<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        Result<TResponse> result = await Send(request);
        return result.ToAcceptedActionResult(this, map);
    }

    protected async Task<IActionResult> HandleNoContent(IRequest<Result> request) {
        Result result = await Send(request);
        return result.ToNoContentActionResult(this);
    }

    private async Task<IActionResult> HandleNoContent(Task<Result> resultTask) {
        Result result = await resultTask;
        return result.ToNoContentActionResult(this);
    }

    protected Task<IActionResult> HandleNoContent(
        IRequest<Result> request,
        Func<Task<Result>, Task<Result>> processResult) =>
        HandleNoContent(processResult(Send(request)));

    protected async Task<IActionResult> HandleFile(IRequest<Result<FileExportResult>> request) {
        Result<FileExportResult> result = await Send(request);
        return result.ToFileActionResult(this);
    }

}
