using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Controllers;

public abstract class BaseApiController(ISender mediator) : ControllerBase {
    protected ISender Mediator { get; } = mediator;

    protected Task Send(IRequest request) {
        return Mediator.Send(request, HttpContext.RequestAborted);
    }

    protected Task<TResponse> Send<TResponse>(IRequest<TResponse> request) {
        return Mediator.Send(request, HttpContext.RequestAborted);
    }

    protected async Task<IActionResult> HandleOk<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        var result = await Send(request);
        return result.ToOkActionResult(this, map);
    }

    protected async Task<IActionResult> HandleCreated<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        string actionName,
        Func<TResponse, object?> routeValues,
        Func<TResponse, THttpResponse> map) {
        var result = await Send(request);
        return result.ToCreatedAtActionResult(this, actionName, routeValues, map);
    }

    protected async Task<IActionResult> HandleNoContent(IRequest<Result> request) {
        var result = await Send(request);
        return result.ToNoContentActionResult();
    }
}
