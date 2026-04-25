using FoodDiary.MailRelay.Presentation.Responses;
using MediatR;
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

    protected async Task<IActionResult> HandleOk<TResponse>(IRequest<TResponse> request) {
        var response = await Send(request);
        return Ok(response);
    }

    protected async Task<IActionResult> HandleOk<TResponse, THttpResponse>(
        IRequest<TResponse> request,
        Func<TResponse, THttpResponse> map) {
        var response = await Send(request);
        return Ok(map(response));
    }

    protected async Task<IActionResult> HandleOk(IRequest request, object response) {
        await Send(request);
        return Ok(response);
    }

    protected async Task<IActionResult> HandleOkOrNotFound<TResponse>(IRequest<TResponse?> request) {
        var response = await Send(request);
        return response is null ? NotFound() : Ok(response);
    }

    protected async Task<IActionResult> HandleOkOrNotFound<TResponse, THttpResponse>(
        IRequest<TResponse?> request,
        Func<TResponse, THttpResponse> map) {
        var response = await Send(request);
        return response is null ? NotFound() : Ok(map(response));
    }

    protected async Task<IActionResult> HandleCreated<TResponse>(
        IRequest<TResponse> request,
        Func<TResponse, string> locationFactory,
        Func<TResponse, object> responseFactory) {
        var response = await Send(request);
        return Created(locationFactory(response), responseFactory(response));
    }

    protected async Task<IActionResult> HandleCreatedOrBadRequest<TResponse, THttpResponse>(
        IRequest<TResponse> request,
        Func<TResponse, string> locationFactory,
        Func<TResponse, THttpResponse> responseFactory) {
        try {
            var response = await Send(request);
            return Created(locationFactory(response), responseFactory(response));
        } catch (InvalidOperationException ex) {
            return BadRequest(CreateErrorResponse("MailRelay.InvalidDeliveryEvent", ex.Message));
        }
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

        var response = await Send(mapped.Request);
        return Created(location, responseFactory(response));
    }

    protected async Task<IActionResult> HandleCreated(
        IRequest request,
        string location,
        object response) {
        await Send(request);
        return Created(location, response);
    }

    protected async Task<IActionResult> HandleAccepted<TResponse>(
        IRequest<TResponse> request,
        Func<TResponse, string> locationFactory,
        Func<TResponse, object> responseFactory) {
        var response = await Send(request);
        return Accepted(locationFactory(response), responseFactory(response));
    }

    protected async Task<IActionResult> HandleNoContentOrNotFound(IRequest<bool> request) {
        var removed = await Send(request);
        return removed ? NoContent() : NotFound();
    }

    private MailRelayApiErrorHttpResponse CreateErrorResponse(string error, string message) =>
        new(error, message, HttpContext.TraceIdentifier);
}
