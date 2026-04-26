using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Presentation.Extensions;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.MailInbox.Presentation.Controllers;

[ApiController]
public abstract class MailInboxControllerBase(ISender sender) : ControllerBase {
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
}
