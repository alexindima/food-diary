using FoodDiary.MailInbox.Presentation.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FoodDiary.MailInbox.Presentation.Filters;

public sealed class MailInboxMessageDetailConcurrencyFilter(MailInboxMessageDetailConcurrencyGate gate)
    : IAsyncResourceFilter {
    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next) {
        using IDisposable? lease = await gate.TryEnterAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (lease is null) {
            context.HttpContext.Response.Headers.RetryAfter = "1";
            context.Result = new ObjectResult(new MailInboxApiErrorHttpResponse(
                "MailInbox.MessageDetailCapacityExceeded",
                "Message detail capacity is temporarily exhausted.",
                context.HttpContext.TraceIdentifier)) {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }

        await next().ConfigureAwait(false);
    }
}
