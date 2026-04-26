using FoodDiary.MailRelay.Application.Common.Result;
using FoodDiary.MailRelay.Presentation.Extensions;
using FoodDiary.MailRelay.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailRelay.Presentation.Controllers;

[ApiController]
public abstract class MailRelayControllerBase(ISender sender) : ControllerBase {
    protected Task Send(IRequest request) {
        return sender.Send(request, HttpContext.RequestAborted);
    }

    protected Task<TResponse> Send<TResponse>(IRequest<TResponse> request) {
        return sender.Send(request, HttpContext.RequestAborted);
    }

    protected async Task<IActionResult> HandleOk<TResponse, THttpResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, THttpResponse> map) {
        var result = await Send(request);
        return result.ToOkActionResult(this, map);
    }

    protected async Task<IActionResult> HandleOk(IRequest<Result> request, object response) {
        var result = await Send(request);
        return result.ToOkActionResult(this, response);
    }

    protected async Task<IActionResult> HandleCreated<TResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, string> locationFactory,
        Func<TResponse, object> responseFactory) {
        var result = await Send(request);
        return result.ToCreatedActionResult(this, locationFactory, responseFactory);
    }

    protected async Task<IActionResult> HandleCreated<TInput, TResponse, THttpResponse>(
        TInput input,
        Func<TInput, MailRelayMappedRequest<TResponse>> map,
        string location,
        Func<TResponse, THttpResponse> responseFactory) {
        var mapped = map(input);
        if (!mapped.IsSuccess || mapped.Request is null) {
            return BadRequest(CreateErrorResponse(
                "MailRelay.ProviderWebhook.InvalidPayload",
                mapped.Error ?? "The provider webhook payload is invalid."));
        }

        var result = await Send(mapped.Request);
        return result.ToCreatedActionResult(this, _ => location, responseFactory);
    }

    protected async Task<IActionResult> HandleCreated(
        IRequest<Result> request,
        string location,
        object response) {
        var result = await Send(request);
        return result.IsSuccess
            ? Created(location, response)
            : MailRelayResultExtensions.ErrorResult(result.Error!, HttpContext.TraceIdentifier);
    }

    protected async Task<IActionResult> HandleAccepted<TResponse>(
        IRequest<Result<TResponse>> request,
        Func<TResponse, string> locationFactory,
        Func<TResponse, object> responseFactory) {
        var result = await Send(request);
        return result.ToAcceptedActionResult(this, locationFactory, responseFactory);
    }

    protected async Task<IActionResult> HandleNoContent(IRequest<Result> request) {
        var result = await Send(request);
        return result.ToNoContentActionResult(this);
    }

    private MailRelayApiErrorHttpResponse CreateErrorResponse(string error, string message) =>
        new(error, message, HttpContext.TraceIdentifier);
}
