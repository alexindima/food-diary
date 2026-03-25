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
}
